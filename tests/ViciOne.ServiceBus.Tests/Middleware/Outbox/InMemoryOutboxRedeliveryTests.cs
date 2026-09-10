using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class InMemoryOutboxRedeliveryTests
{
    private static readonly TimeSpan RedeliveryInterval = TimeSpan.FromHours(1);

    [Theory]
    [InlineData(RedeliveryConfiguration.ConsumerMessage, DeferredOperation.Publish)]
    [InlineData(RedeliveryConfiguration.ConsumerMessage, DeferredOperation.Send)]
    [InlineData(RedeliveryConfiguration.Endpoint, DeferredOperation.Publish)]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-REDELIVERY", "failed-attempts-never-flush-outbox")]
    public async Task FailedDelivery_NeverFlushesItsOutboxAcrossImmediateAndDelayedAttemptsAsync(
        RedeliveryConfiguration configurationShape,
        DeferredOperation deferredOperation)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RedeliveryObservation(deferredOperation);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<AlwaysFaultingOutboxConsumer>((_, consumer) =>
                {
                    if (configurationShape == RedeliveryConfiguration.ConsumerMessage)
                    {
                        consumer.Message<OutboxCommand>(message =>
                        {
                            message.UseDelayedRedelivery(redelivery =>
                                redelivery.Intervals(RedeliveryInterval));
                            message.UseMessageRetry(retry => retry.Immediate(1));
                            message.UseVolatileOutbox();
                        });
                    }
                });
                if (configurationShape == RedeliveryConfiguration.Endpoint)
                {
                    configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    {
                        endpoint.UseDelayedRedelivery(redelivery =>
                            redelivery.Intervals(RedeliveryInterval));
                        endpoint.UseMessageRetry(retry => retry.Immediate(1));
                        endpoint.UseVolatileOutbox();
                    });
                }
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var scheduledObserver = new ScheduledRedeliveryObserver();
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduledObserver);

        try
        {
            Task<IPublishedMessage<Fault<OutboxCommand>>> fault = harness.Published
                .SelectAsync<Fault<OutboxCommand>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Guid messageId = NewId.NextGuid();

            await harness.Bus.PublishAsync(new OutboxCommand(messageId), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            SendContext scheduled = await scheduledObserver.Scheduled.WaitAsync(timeout, cancellationToken);

            Assert.Equal(RedeliveryInterval, scheduled.Delay);
            Assert.Equal(2, observation.Attempts.Length);
            Assert.Equal([0, 0], observation.Attempts.Select(attempt => attempt.RedeliveryCount));
            Assert.Equal(0, observation.SideEffectCount);

            IInMemoryDelayProvider delayProvider =
                harness.Scope.ServiceProvider.GetRequiredService<IInMemoryDelayProvider>();
            delayProvider.Advance(RedeliveryInterval);
            IPublishedMessage<Fault<OutboxCommand>> publishedFault = await fault.WaitAsync(
                timeout,
                cancellationToken);

            Assert.Equal(messageId, publishedFault.Context.Message.Message.Id);
            Assert.Equal(4, observation.Attempts.Length);
            Assert.Equal([0, 0, 1, 1], observation.Attempts.Select(attempt => attempt.RedeliveryCount));
            Assert.Equal(0, observation.SideEffectCount);
            Assert.Empty(harness.Sent.Snapshot<OutboxSideEffect>());
            Assert.Empty(harness.Published.Snapshot<OutboxSideEffect>());
            Assert.Single(harness.Published.Snapshot<Fault<OutboxCommand>>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None)
                .WaitAsync(timeout, CancellationToken.None);
        }
    }


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum RedeliveryConfiguration
    {
        ConsumerMessage,
        Endpoint,
    }

    public enum DeferredOperation
    {
        Publish,
        Send,
    }

    public sealed record OutboxCommand(Guid Id);

    public sealed record OutboxSideEffect(Guid Id);

    private sealed class AlwaysFaultingOutboxConsumer(RedeliveryObservation observation) :
        IConsumer<OutboxCommand>,
        IConsumer<OutboxSideEffect>
    {
        public async Task ConsumeAsync(ConsumeContext<OutboxCommand> context)
        {
            observation.RecordAttempt(context.Advanced().GetRedeliveryCount());
            if (observation.DeferredOperation == DeferredOperation.Publish)
                await context.Advanced().PublishAsync(new OutboxSideEffect(context.Message.Id));
            else
                await context.Advanced().SendAsync(context.Advanced().ReceiveContext.InputAddress, new OutboxSideEffect(context.Message.Id));

            throw new ExpectedOutboxFailureException();
        }

        public Task ConsumeAsync(ConsumeContext<OutboxSideEffect> context)
        {
            observation.RecordSideEffect();
            return Task.CompletedTask;
        }
    }

    private sealed class RedeliveryObservation(DeferredOperation deferredOperation)
    {
        private readonly List<Attempt> _attempts = [];
        private readonly object _lock = new();
        private int _sideEffectCount;

        public DeferredOperation DeferredOperation { get; } = deferredOperation;

        public Attempt[] Attempts
        {
            get
            {
                lock (_lock)
                    return _attempts.ToArray();
            }
        }

        public int SideEffectCount => Volatile.Read(ref _sideEffectCount);

        public void RecordAttempt(int redeliveryCount)
        {
            lock (_lock)
                _attempts.Add(new Attempt(redeliveryCount));
        }

        public void RecordSideEffect() => Interlocked.Increment(ref _sideEffectCount);
    }

    private sealed record Attempt(int RedeliveryCount);

    private sealed class ScheduledRedeliveryObserver : ISendObserver
    {
        private readonly TaskCompletionSource<SendContext> _scheduled = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SendContext> Scheduled => _scheduled.Task;

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (typeof(T) == typeof(OutboxCommand) && context.Delay.HasValue)
                _scheduled.TrySetResult(context);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (typeof(T) == typeof(OutboxCommand) && context.Delay.HasValue)
                _scheduled.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedOutboxFailureException : Exception;
}
