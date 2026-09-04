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
    public async Task TransportDeliveryFailure_RetainsOnlyTheUndeliveredTailAcrossImmediateRetryAsync()
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
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Guid commandId = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        var completion = new ReceiveCompletionObserver(messageId);
        using ConnectHandle observer = harness.Bus.ConnectReceiveObserver(completion);
        using ConnectHandle sendObserver = harness.Bus.ConnectSendObserver(new FailFirstSecondSideEffectSendObserver(observation));

        try
        {
            await harness.Bus.PublishAsync(
                new DeliveryCommand(commandId),
                context => context.MessageId = messageId,
                cancellationToken);
            await completion.Completed.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, observation.CommandExecutions);
        Assert.Equal(1, observation.InjectedDeliveryFailures);
        Assert.Equal(1, observation.FirstSuccessfulSends);
        Assert.Equal(1, observation.SecondSuccessfulSends);
    }

    public sealed record DeliveryCommand(Guid Id);

    public sealed record FirstSideEffect(Guid Id);

    public sealed record SecondSideEffect(Guid Id);

    private sealed class DeliveryConsumer(DeliveryObservation observation) :
        IConsumer<DeliveryCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<DeliveryCommand> context)
        {
            observation.RecordCommandExecution();
            await context.Advanced().SendAsync(context.Advanced().ReceiveContext.InputAddress, new FirstSideEffect(context.Message.Id));
            await context.Advanced().SendAsync(context.Advanced().ReceiveContext.InputAddress, new SecondSideEffect(context.Message.Id));
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
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.SupportedMessageTypes.Contains(
                    MessageUrn.ForTypeString<SecondSideEffect>(),
                    StringComparer.Ordinal)
                && observation.TryInjectDeliveryFailure())
                return Task.FromException(new ExpectedDeliveryFailureException());

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            observation.RecordSuccessfulSend(context.SupportedMessageTypes);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed class ReceiveCompletionObserver(Guid messageId) : IReceiveObserver
    {
        private readonly TaskCompletionSource _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                _completed.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            if (context.GetMessageId() == messageId)
                _completed.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class DeliveryObservation
    {
        private int _commandExecutions;
        private int _deliveryFailureInjected;
        private int _firstSuccessfulSends;
        private int _secondSuccessfulSends;

        public int CommandExecutions => Volatile.Read(ref _commandExecutions);

        public int InjectedDeliveryFailures => Volatile.Read(ref _deliveryFailureInjected);

        public int FirstSuccessfulSends => Volatile.Read(ref _firstSuccessfulSends);

        public int SecondSuccessfulSends => Volatile.Read(ref _secondSuccessfulSends);

        public void RecordCommandExecution() => Interlocked.Increment(ref _commandExecutions);

        public void RecordSuccessfulSend(string[] messageTypes)
        {
            if (messageTypes.Contains(MessageUrn.ForTypeString<FirstSideEffect>(), StringComparer.Ordinal))
                Interlocked.Increment(ref _firstSuccessfulSends);
            if (messageTypes.Contains(MessageUrn.ForTypeString<SecondSideEffect>(), StringComparer.Ordinal))
                Interlocked.Increment(ref _secondSuccessfulSends);
        }

        public bool TryInjectDeliveryFailure() =>
            Interlocked.CompareExchange(ref _deliveryFailureInjected, 1, 0) == 0;
    }

    private sealed class ExpectedDeliveryFailureException : Exception;
}
