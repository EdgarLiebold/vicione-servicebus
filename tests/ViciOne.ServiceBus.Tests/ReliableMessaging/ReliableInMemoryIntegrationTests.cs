using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.ReliableMessaging;

public sealed class ReliableInMemoryIntegrationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "active-cancellation-preserves-token-without-retry-quarantine-or-outbox-delivery")]
    public async Task ConsumerCancellation_PreservesTheOriginalTokenWithoutRetryQuarantineOrOutboxDeliveryAsync(int maximumAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();
        var probe = new CancellationProbe(cancellation);
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration, maximumAttempts);
                configuration.AddConsumer<CancellationConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0),
                send => send.MessageId = messageId, cancellationToken);
            Exception? observed = await probe.Completed.Task.WaitAsync(timeout, cancellationToken);
            OperationCanceledException failure = Assert.IsType<OperationCanceledException>(observed);
            Assert.Same(probe.Failure, failure);
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(1, probe.PreparedMessages);
            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult retained = await inbox.AcquireAsync(
                new ReliableInboxKey(messageId, probe.ConsumerId), probe.StartedAt,
                TimeSpan.FromMinutes(1), cancellationToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Busy, retained.Disposition);
            Assert.Equal(1, retained.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), cancellationToken)).Entries);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
        DurableSendStoreSnapshot snapshot = await provider.GetRequiredService<IOutboxStore<IBus>>()
            .GetSnapshotAsync(cancellationToken);
        Assert.Equal(0, snapshot.StoredCount);
        Assert.Equal(0, snapshot.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "concurrent-duplicate-message-id-executes-body-once")]
    public async Task InboxLock_AllowsExactlyOneOfThreeConcurrentDeliveriesToPublishTheHundredEventsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new InboxObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<InboxConsumer, InboxConsumerDefinition>();
                configuration.AddConsumer<InboxEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            Task[] deliveries = Enumerable.Range(0, 3)
                .Select(_ => harness.Bus.PublishAsync(
                    new InboxCommand(),
                    context => context.MessageId = messageId,
                    cancellationToken))
                .ToArray();
            await Task.WhenAll(deliveries).WaitAsync(timeout, cancellationToken);
            await observation.AllEvents.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, observation.ConsumerExecutions);
        Assert.Equal(100, observation.Events.Count);
        Assert.Equal(Enumerable.Range(0, 100).Select(index => $"{index:0000}"), observation.Events.Keys.Order());
        Assert.All(observation.Events.Values, count => Assert.Equal(1, count));
        Assert.Equal(100, harness.Consumed.Snapshot<InboxEvent>().Count());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [RequirementCoverage("REQ-VSB-RELIABLE-CONSUMER", "outbox-exactly-once-across-success-and-first-attempt-retry")]
    public async Task ConsumerOutbox_PublishesBothScopedEventsExactlyOnceWithTheirRoutingKeysAsync(
        int failuresBeforeSuccess,
        int expectedAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<IReliablePublisher, ReliablePublisher>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<ReliableConsumer, ReliableConsumerDefinition>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            await harness.Bus.PublishAsync(
                new ReliableCommand(messageId, failuresBeforeSuccess),
                context => context.MessageId = messageId,
                cancellationToken);
            await observation.BothEvents.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(expectedAttempts, observation.ConsumerAttempts);
        Assert.Equal(new[] { "First", "Second" }, observation.Events.Keys.Order());
        Assert.All(observation.Events.Values, count => Assert.Equal(1, count));
        Assert.Equal(new[] { "alpha", "beta" }, observation.RoutingKeys.Order());
        ReliableEvent[] events = harness.Consumed.Snapshot<ReliableEvent>()
            .Select(message => message.Context.Message)
            .Where(message => message.MessageId == messageId)
            .ToArray();
        Assert.Equal(2, events.Length);
        Assert.Single(events, message => message.Text == "First");
        Assert.Single(events, message => message.Text == "Second");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "repeated-failures-advance-attempts-and-reach-quarantine")]
    public async Task ConsumerInbox_RepeatedFailuresAdvanceAttemptsAndReachQuarantineAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<IReliablePublisher, ReliablePublisher>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<ReliableConsumer, ReliableConsumerDefinition>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            await harness.Bus.PublishAsync(
                new ReliableCommand(messageId, int.MaxValue),
                context => context.MessageId = messageId,
                cancellationToken);
            await harness.InactivityTask.WaitAsync(timeout, cancellationToken);

            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxQuarantineEntry quarantined = Assert.Single((await inbox.GetQuarantineAsync(
                new ReliableInboxQuarantineQuery { PageSize = 1 },
                cancellationToken)).Entries);
            Assert.Equal(messageId, quarantined.Key.MessageId);
            Assert.Equal(ReliableInboxStatus.Quarantined, quarantined.Status);
            Assert.Equal(3, quarantined.Attempts);
            Assert.Equal(typeof(ExpectedReliableException).FullName, quarantined.FailureType);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(3, observation.ConsumerAttempts);
        Assert.Empty(observation.Events);
        Assert.Empty(harness.Published.Snapshot<Fault<ReliableCommand>>());
    }

    [Theory]
    [InlineData(ReliableSagaFailure.None, 1)]
    [InlineData(ReliableSagaFailure.FirstConsumeAttempt, 2)]
    [InlineData(ReliableSagaFailure.FirstDeliveryAttempt, 1)]
    [RequirementCoverage("REQ-VSB-RELIABLE-SAGA", "success-consume-retry-and-delivery-redelivery-reach-verified")]
    public async Task SagaOutbox_ReachesVerifiedWithOneCommittedStateMessageAsync(
        ReliableSagaFailure failure,
        int expectedCreateAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        var services = new ServiceCollection();
        services.AddSingleton(observation);
        services.AddSingleton<ITransportSendFailureClassifier, ExpectedReliableFailureClassifier>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            ConfigureReliableMessaging(configuration);
            configuration.AddSagaStateMachine<ReliableMachine, ReliableState, ReliableStateDefinition>()
                .InMemoryRepository();
        });
        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        ISagaStateMachineTestHarness<ReliableMachine, ReliableState> sagaHarness =
            harness.GetSagaStateMachineHarness<ReliableMachine, ReliableState>();
        Guid correlationId = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        using ConnectHandle? deliveryFailure = failure == ReliableSagaFailure.FirstDeliveryAttempt
            ? harness.Bus.ConnectSendObserver(new FailFirstReliableStateVerifiedSendObserver(observation))
            : null;

        try
        {
            await harness.Bus.PublishAsync(
                new CreateReliableState(correlationId, failure),
                context => context.MessageId = messageId,
                cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, state => state.Verified, timeout, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(expectedCreateAttempts, observation.SagaCreateAttempts);
        Assert.Equal(failure == ReliableSagaFailure.FirstDeliveryAttempt ? 1 : 0, observation.SagaDeliveryFailures);
        Assert.Single(harness.Consumed.Snapshot<ReliableStateVerified>());
        Assert.Empty(harness.Published.Snapshot<Fault<CreateReliableState>>());
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static void ConfigureReliableMessaging(IBusRegistrationConfigurator configuration, int maximumAttempts = 3)
    {
        configuration.Limits(MessageLimits.Conservative);
        configuration.UseReliableMessaging(reliable =>
        {
            reliable.UseInMemoryStore();
            reliable.Store(new ReliableStoreLimits
            {
                MaximumStoredCount = 1_000,
                MaximumStoredBytes = 16 * 1024 * 1024,
            });
            reliable.Delivery(delivery =>
            {
                delivery.MaximumAttempts = maximumAttempts;
                delivery.InitialRetryDelay = TimeSpan.FromMilliseconds(20);
                delivery.MaximumRetryDelay = TimeSpan.FromMilliseconds(50);
                delivery.RetryJitterFraction = 0;
                delivery.PollInterval = TimeSpan.FromMilliseconds(10);
            });
            reliable.Retention(TimeSpan.FromDays(1));
            reliable.AddMessageContract<InboxCommand>("inbox-command");
            reliable.AddMessageContract<InboxEvent>("inbox-event");
            reliable.AddMessageContract<ReliableCommand>("reliable-command");
            reliable.AddMessageContract<ReliableEvent>("reliable-event");
            reliable.AddMessageContract<CreateReliableState>("create-reliable-state");
            reliable.AddMessageContract<ReliableStateVerified>("reliable-state-verified");
        });
    }


    public sealed class CancellationProbe(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Guid ConsumerId { get; } = Guid.NewGuid();
        public DateTimeOffset StartedAt { get; set; }
        public int PreparedMessages { get; set; }
        public OperationCanceledException Failure { get; } = new(cancellation.Token);
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class CancellationConsumer(IServiceProvider provider, CancellationProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                probe.StartedAt = DateTimeOffset.UtcNow;
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId, async inner =>
                {
                    await inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, "canceled-intent"),
                        probe.Cancellation.Token);
                    probe.PreparedMessages++;
                    probe.Cancellation.Cancel();
                    throw probe.Failure;
                }, probe.Cancellation.Token);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class InboxObservation
    {
        int _consumerExecutions;

        public int ConsumerExecutions => Volatile.Read(ref _consumerExecutions);

        public ConcurrentDictionary<string, int> Events { get; } = new(StringComparer.Ordinal);

        public TaskCompletionSource AllEvents { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ConsumerExecuted() => Interlocked.Increment(ref _consumerExecutions);

        public void EventReceived(string text)
        {
            Events.AddOrUpdate(text, 1, static (_, count) => count + 1);
            if (Events.Count == 100)
                AllEvents.TrySetResult();
        }
    }

    public sealed class ReliableObservation
    {
        int _consumerAttempts;
        int _sagaCreateAttempts;
        int _sagaDeliveryFailures;

        public int ConsumerAttempts => Volatile.Read(ref _consumerAttempts);

        public int SagaCreateAttempts => Volatile.Read(ref _sagaCreateAttempts);

        public int SagaDeliveryFailures => Volatile.Read(ref _sagaDeliveryFailures);

        public ConcurrentDictionary<string, int> Events { get; } = new(StringComparer.Ordinal);

        public ConcurrentBag<string> RoutingKeys { get; } = [];

        public TaskCompletionSource BothEvents { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConsumerAttempt() => Interlocked.Increment(ref _consumerAttempts);

        public int SagaCreateAttempt() => Interlocked.Increment(ref _sagaCreateAttempts);

        public bool TryInjectSagaDeliveryFailure() =>
            Interlocked.CompareExchange(ref _sagaDeliveryFailures, 1, 0) == 0;

        public void EventReceived(string text, string routingKey)
        {
            Events.AddOrUpdate(text, 1, static (_, count) => count + 1);
            RoutingKeys.Add(routingKey);
            if (Events.Count == 2)
                BothEvents.TrySetResult();
        }
    }

    public sealed record InboxCommand;

    public sealed record InboxEvent(Guid MessageId, string Text);

    public sealed class InboxConsumer(InboxObservation observation) : IConsumer<InboxCommand>
    {
        public Task ConsumeAsync(ConsumeContext<InboxCommand> context)
        {
            observation.ConsumerExecuted();
            return Task.WhenAll(Enumerable.Range(0, 100).Select(index => context.Advanced().PublishAsync(
                new InboxEvent(context.MessageId!.Value, $"{index:0000}"),
                context.CancellationToken)));
        }
    }

    public sealed class InboxEventConsumer(InboxObservation observation) : IConsumer<InboxEvent>
    {
        public Task ConsumeAsync(ConsumeContext<InboxEvent> context)
        {
            observation.EventReceived(context.Message.Text);
            return Task.CompletedTask;
        }
    }

    public sealed class InboxConsumerDefinition : ConsumerDefinition<InboxConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<InboxConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            _ = endpointConfigurator;
            _ = consumerConfigurator;
            _ = context;
        }
    }

    public sealed record ReliableCommand(Guid MessageId, int FailuresBeforeSuccess);

    public sealed record ReliableEvent(Guid MessageId, string Text);

    public interface IReliablePublisher
    {
        Task PublishSecondAsync(Guid messageId, CancellationToken cancellationToken);
    }

    public sealed class ReliablePublisher(IPublishEndpoint publishEndpoint) : IReliablePublisher
    {
        public Task PublishSecondAsync(Guid messageId, CancellationToken cancellationToken) => publishEndpoint.PublishAsync(
            new ReliableEvent(messageId, "Second"),
            context => context.SetRoutingKey("beta"),
            cancellationToken);
    }

    public sealed class ReliableConsumer(
        ReliableObservation observation,
        IReliablePublisher publisher) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            int attempt = observation.ConsumerAttempt();
            await context.Advanced().PublishAsync(
                new ReliableEvent(context.Message.MessageId, "First"),
                publish => publish.SetRoutingKey("alpha"));
            await publisher.PublishSecondAsync(context.Message.MessageId, context.CancellationToken);
            if (attempt <= context.Message.FailuresBeforeSuccess)
                throw new ExpectedReliableException("expected consumer failure");
        }
    }

    public sealed class ReliableEventConsumer(ReliableObservation observation) : IConsumer<ReliableEvent>
    {
        public Task ConsumeAsync(ConsumeContext<ReliableEvent> context)
        {
            observation.EventReceived(context.Message.Text, context.Advanced().GetRoutingKey() ?? string.Empty);
            return Task.CompletedTask;
        }
    }

    public sealed class ReliableConsumerDefinition : ConsumerDefinition<ReliableConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ReliableConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            _ = endpointConfigurator;
            _ = consumerConfigurator;
            _ = context;
        }
    }

    public enum ReliableSagaFailure
    {
        None,
        FirstConsumeAttempt,
        FirstDeliveryAttempt,
    }

    public sealed record CreateReliableState(Guid CorrelationId, ReliableSagaFailure Failure) : ICorrelatedBy<Guid>;

    public sealed record ReliableStateVerified(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ReliableState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ReliableMachine : ViciOneServiceBusStateMachine<ReliableState>
    {
        public ReliableMachine(ReliableObservation observation)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Create, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });
            Initially(When(Create)
                .Then(context =>
                {
                    int attempt = observation.SagaCreateAttempt();
                    if (context.Message.Failure == ReliableSagaFailure.FirstConsumeAttempt && attempt == 1)
                        throw new ExpectedReliableException("first saga consume attempt");
                })
                .TransitionTo(Created)
                .Send(
                    context => context.ReceiveContext.InputAddress,
                    context => new ReliableStateVerified(context.Saga.CorrelationId)));
            During(Created, When(VerifiedEvent).TransitionTo(Verified));
        }

        public IState Created { get; private set; } = null!;

        public IState Verified { get; private set; } = null!;

        public IEvent<CreateReliableState> Create { get; private set; } = null!;

        public IEvent<ReliableStateVerified> VerifiedEvent { get; private set; } = null!;
    }

    public sealed class ReliableStateDefinition : SagaDefinition<ReliableState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ReliableState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageScope(context);
            _ = sagaConfigurator;
        }
    }

    private sealed class FailFirstReliableStateVerifiedSendObserver(ReliableObservation observation) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.SupportedMessageTypes.Contains(
                    MessageUrn.ForTypeString<ReliableStateVerified>(),
                    StringComparer.Ordinal)
                && observation.TryInjectSagaDeliveryFailure())
                return Task.FromException(new ExpectedReliableException("first saga delivery attempt"));

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    public sealed class ExpectedReliableException(string message) : Exception(message);

    private sealed class ExpectedReliableFailureClassifier : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind kind)
        {
            ArgumentNullException.ThrowIfNull(exception);
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is not ExpectedReliableException)
                    continue;

                kind = TransportSendFailureKind.Transient;
                return true;
            }

            kind = TransportSendFailureKind.Unclassified;
            return false;
        }
    }
}
