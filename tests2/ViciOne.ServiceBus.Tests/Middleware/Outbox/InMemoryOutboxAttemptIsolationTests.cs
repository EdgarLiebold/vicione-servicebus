using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class InMemoryOutboxAttemptIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-ATTEMPT", "delivery-failure-retains-undelivered-tail")]
    public async Task TransportDeliveryFailure_RetainsOnlyTheUndeliveredTailAcrossImmediateRetry()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new DeliveryObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddInMemoryInboxOutbox()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DeliveryConsumer, DeliveryConsumerDefinition>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        Guid commandId = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        var completion = new ReceiveCompletionObserver(messageId);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(completion);
        using ConnectHandle sendObserver = harness.Bus.ConnectSendObserver(new FailFirstSecondSideEffectSendObserver(observation));

        try
        {
            await harness.Bus.Publish(
                new DeliveryCommand(commandId),
                context => context.MessageId = messageId,
                cancellationToken);
            await completion.Completed.WaitAsync(timeout, cancellationToken);
            await observation.AllSideEffectsReceived.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, observation.CommandExecutions);
        Assert.Equal(1, observation.InjectedDeliveryFailures);
        Assert.Equal(
            new[] { $"first:{commandId}", $"second:{commandId}" },
            observation.SideEffects.Order(StringComparer.Ordinal));
        Assert.All(observation.SideEffectCounts.Values, count => Assert.Equal(1, count));
        Assert.Single(harness.Consumed.Select<FirstSideEffect>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<SecondSideEffect>(SnapshotOnlyToken()));
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record DeliveryCommand(Guid Id);

    public sealed record FirstSideEffect(Guid Id);

    public sealed record SecondSideEffect(Guid Id);

    private sealed class DeliveryConsumer(DeliveryObservation observation) :
        IConsumer<DeliveryCommand>,
        IConsumer<FirstSideEffect>,
        IConsumer<SecondSideEffect>
    {
        public async Task Consume(ConsumeContext<DeliveryCommand> context)
        {
            observation.RecordCommandExecution();
            await context.Send(context.ReceiveContext.InputAddress, new FirstSideEffect(context.Message.Id));
            await context.Send(context.ReceiveContext.InputAddress, new SecondSideEffect(context.Message.Id));
        }

        public Task Consume(ConsumeContext<FirstSideEffect> context)
        {
            observation.RecordSideEffect($"first:{context.Message.Id}");
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SecondSideEffect> context)
        {
            observation.RecordSideEffect($"second:{context.Message.Id}");
            return Task.CompletedTask;
        }
    }

    private sealed class DeliveryConsumerDefinition : ConsumerDefinition<DeliveryConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<DeliveryConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(1));
            endpointConfigurator.UseInMemoryInboxOutbox(context);
        }
    }

    private sealed class FailFirstSecondSideEffectSendObserver(DeliveryObservation observation) : ISendObserver
    {
        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            if (context.SupportedMessageTypes.Contains(
                    MessageUrn.ForTypeString<SecondSideEffect>(),
                    StringComparer.Ordinal)
                && observation.TryInjectDeliveryFailure())
                return Task.FromException(new ExpectedDeliveryFailureException());

            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed class ReceiveCompletionObserver(Guid messageId) : IReceiveObserver
    {
        private readonly TaskCompletionSource _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;

        public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceive(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                _completed.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            if (context.GetMessageId() == messageId)
                _completed.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class DeliveryObservation
    {
        private readonly TaskCompletionSource _allSideEffectsReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentDictionary<string, int> _sideEffectCounts = new(StringComparer.Ordinal);
        private int _commandExecutions;
        private int _deliveryFailureInjected;

        public Task AllSideEffectsReceived => _allSideEffectsReceived.Task;

        public int CommandExecutions => Volatile.Read(ref _commandExecutions);

        public int InjectedDeliveryFailures => Volatile.Read(ref _deliveryFailureInjected);

        public IReadOnlyDictionary<string, int> SideEffectCounts => _sideEffectCounts;

        public string[] SideEffects => _sideEffectCounts.Keys.ToArray();

        public void RecordCommandExecution() => Interlocked.Increment(ref _commandExecutions);

        public void RecordSideEffect(string identity)
        {
            _sideEffectCounts.AddOrUpdate(identity, 1, static (_, count) => count + 1);
            if (_sideEffectCounts.Count == 2)
                _allSideEffectsReceived.TrySetResult();
        }

        public bool TryInjectDeliveryFailure() =>
            Interlocked.CompareExchange(ref _deliveryFailureInjected, 1, 0) == 0;
    }

    private sealed class ExpectedDeliveryFailureException : Exception;
}
