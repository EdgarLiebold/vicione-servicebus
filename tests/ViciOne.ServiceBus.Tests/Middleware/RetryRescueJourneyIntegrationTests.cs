using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RetryRescueJourneyIntegrationTests
{
    [Theory]
    [InlineData(FailureRoute.Rescued)]
    [InlineData(FailureRoute.Ignored)]
    [RequirementCoverage("REQ-VSB-RESCUE", "retry-exhaustion-rescue-and-unhandled-failure")]
    public async Task ConsumerFailure_RetriesBeforeItsSelectedRescueAndPreservesNeighborDeliveryAsync(FailureRoute route)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new FailureObservation(route);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<FailingConsumer>();
                configuration.AddConsumer<HealthyConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    IPipe<ExceptionConsumeContext> rescue = Pipe.New<ExceptionConsumeContext>(pipe =>
                        pipe.UseExecuteAwaited(observation.RecordRescueAsync));
                    bus.UseRescue(rescue, exceptions => exceptions.Handle<OriginalFailureException>());
                    RetryConfigurationExtensions.UseMessageRetry(
                        (IPipeConfigurator<ConsumeContext>)bus,
                        bus,
                        retry => retry.Immediate(2));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);

        try
        {
            Guid failureId = NewId.NextGuid();
            Guid healthyId = NewId.NextGuid();
            Task<IPublishedMessage<Fault<FailureMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<FailureMessage>>(
                    message => message.Context.Message.Message.CorrelationId == failureId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken);

            await harness.Bus.PublishAsync(new FailureMessage(failureId), cancellationToken);
            await harness.Bus.PublishAsync(new HealthyMessage(healthyId), cancellationToken);
            Assert.Equal(healthyId, await observation.Healthy.Task.WaitAsync(timeout, cancellationToken));

            Fault<FailureMessage> fault = (await faultTask.WaitAsync(timeout, cancellationToken)).Context.Message;
            if (route == FailureRoute.Rescued)
                await observation.Rescued.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal([failureId, failureId, failureId], observation.Attempts.ToArray());
            Assert.Equal(failureId, fault.Message.CorrelationId);
            if (route == FailureRoute.Ignored)
            {
                Assert.Equal(0, observation.RescueCount);
                Assert.Equal(TypeCache<IgnoredFailureException>.ShortName, Assert.Single(fault.Exceptions).ExceptionType);
            }
            else
            {
                Assert.Equal(1, observation.RescueCount);
                Assert.Same(observation.Original, observation.RescuedException);
                Assert.Equal(failureId, observation.RecoveredId);
                Assert.Equal(TypeCache<OriginalFailureException>.ShortName, Assert.Single(fault.Exceptions).ExceptionType);
            }
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<Fault<FailureMessage>>());
        if (route == FailureRoute.Rescued)
            Assert.Equal(observation.RecoveredId, Assert.Single(harness.Published.Snapshot<RecoveredMessage>()).Context.Message.CorrelationId);
        else
            Assert.Empty(harness.Published.Snapshot<RecoveredMessage>());
    }

    public enum FailureRoute
    {
        Rescued,
        Ignored,
    }

    public sealed record FailureMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record HealthyMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RecoveredMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class FailureObservation(FailureRoute route)
    {
        private int _rescueCount;

        public OriginalFailureException Original { get; } = new("original-failure");

        public IgnoredFailureException Ignored { get; } = new("ignored-failure");

        public ConcurrentQueue<Guid> Attempts { get; } = new();

        public TaskCompletionSource<Exception> Rescued { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Guid> Healthy { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RescueCount => Volatile.Read(ref _rescueCount);

        public Exception? RescuedException { get; private set; }

        public Guid RecoveredId { get; private set; }

        public Exception Failure => route == FailureRoute.Ignored ? Ignored : Original;

        public async Task RecordRescueAsync(ExceptionConsumeContext context)
        {
            RescuedException = context.Exception;
            Interlocked.Increment(ref _rescueCount);
            RecoveredId = context.CorrelationId
                ?? throw new InvalidOperationException("Rescued message has no correlation identifier.");
            await context.Advanced().PublishAsync(new RecoveredMessage(RecoveredId), context.CancellationToken);
            Rescued.TrySetResult(context.Exception);
        }
    }

    public sealed class FailingConsumer(FailureObservation observation) : IConsumer<FailureMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FailureMessage> context)
        {
            observation.Attempts.Enqueue(context.Message.CorrelationId);
            throw observation.Failure;
        }
    }

    public sealed class HealthyConsumer(FailureObservation observation) : IConsumer<HealthyMessage>
    {
        public Task ConsumeAsync(ConsumeContext<HealthyMessage> context)
        {
            observation.Healthy.TrySetResult(context.Message.CorrelationId);
            return Task.CompletedTask;
        }
    }

    public sealed class OriginalFailureException(string message) : Exception(message);

    public sealed class IgnoredFailureException(string message) : Exception(message);
}
