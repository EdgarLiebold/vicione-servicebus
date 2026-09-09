using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class ReliableTransactionalOutboxTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RELIABLE-CONSUMER", "successful-consume-commits-complete-event-set-once")]
    public async Task ReliableConsumer_SuccessCommitsTheCompleteEventSetExactlyOnceAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync();
        var command = new ReliableCommand(NewId.NextGuid(), FailFirstAttempt: false);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        ReliableEvent first = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ReliableEvent second = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ReliableEventSnapshot[] events = fixture.Deliveries.For(command.CorrelationId);

        Assert.Equal(command.CorrelationId, first.CorrelationId);
        Assert.Equal(command.CorrelationId, second.CorrelationId);
        Assert.Equal(1, fixture.ConsumerAttempts.For(command.CorrelationId));
        Assert.Equal(2, events.Length);
        Assert.Equal(["First", "Second"], events.Select(item => item.Message.Text).Order().ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RELIABLE-CONSUMER", "failed-attempt-rolls-back-before-complete-retry-event-set")]
    public async Task ReliableConsumer_FirstFailureRollsBackBeforeTheRetryCommitsEachEventOnceAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync();
        var command = new ReliableCommand(NewId.NextGuid(), FailFirstAttempt: true);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        ReliableEvent first = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ReliableEvent second = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ReliableEventSnapshot[] events = fixture.Deliveries.For(command.CorrelationId);

        Assert.Equal(command.CorrelationId, first.CorrelationId);
        Assert.Equal(command.CorrelationId, second.CorrelationId);
        Assert.Equal(2, fixture.ConsumerAttempts.For(command.CorrelationId));
        Assert.Equal(2, events.Length);
        Assert.Equal(["First", "Second"], events.Select(item => item.Message.Text).Order().ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RELIABLE-SAGA", "self-addressed-message-commits-terminal-state")]
    public async Task ReliableSaga_CommitsItsSelfAddressedMessageAndTerminalStateAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync();
        var command = new CreateReliableState(NewId.NextGuid(), FailFirstAttempt: false);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        IReceivedMessage<CreateReliableState> created = await fixture.ConsumedAsync(
            command.CorrelationId,
            fixture.CancellationToken);
        IReceivedMessage<StateVerified> verified = await fixture.VerifiedAsync(
            command.CorrelationId,
            fixture.CancellationToken);
        ReliableState state = await fixture.ReadStateAsync(command.CorrelationId);

        Assert.Null(created.Exception);
        Assert.Null(verified.Exception);
        Assert.Equal(created.Context.Advanced().ReceiveContext.InputAddress, verified.Context.Advanced().ReceiveContext.InputAddress);
        Assert.Equal(ReliableStateMachine.VerifiedStateName, state.CurrentState);
        Assert.Equal(1, fixture.SagaAttempts.For(command.CorrelationId));
        Assert.Single(fixture.VerifiedSnapshot(command.CorrelationId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RELIABLE-SAGA", "failed-first-attempt-rolls-back-before-terminal-retry")]
    public async Task ReliableSaga_FirstAttemptFailureRollsBackBeforeOneTerminalRetryAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync();
        var command = new CreateReliableState(NewId.NextGuid(), FailFirstAttempt: true);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        IReceivedMessage<StateVerified> verified = await fixture.VerifiedAsync(
            command.CorrelationId,
            fixture.CancellationToken);
        ReliableState state = await fixture.ReadStateAsync(command.CorrelationId);

        Assert.Null(verified.Exception);
        Assert.Equal(ReliableStateMachine.VerifiedStateName, state.CurrentState);
        Assert.Equal(2, fixture.SagaAttempts.For(command.CorrelationId));
        Assert.Single(fixture.VerifiedSnapshot(command.CorrelationId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-DELIVERY-RECOVERY", "real-send-pipeline-failure-retries-without-duplicate")]
    public async Task TransportSendFailure_RetriesTheCommittedOutboxWithoutDuplicatingTheMessageAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync(failFirstOutboxDelivery: true);
        var command = new CreateReliableState(NewId.NextGuid(), FailFirstAttempt: false);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        IReceivedMessage<StateVerified> verified = await fixture.VerifiedAsync(
            command.CorrelationId,
            fixture.CancellationToken);
        ReliableState state = await fixture.ReadStateAsync(command.CorrelationId);

        Assert.Null(verified.Exception);
        Assert.Equal(ReliableStateMachine.VerifiedStateName, state.CurrentState);
        Assert.Equal(1, fixture.SagaAttempts.For(command.CorrelationId));
        Assert.Equal(2, fixture.DeliveryFailures.AttemptCount);
        Assert.Single(fixture.VerifiedSnapshot(command.CorrelationId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-TRANSPORT-PROPERTIES", "routing-key-round-trips-through-persistence")]
    public async Task RoutingKeys_RoundTripThroughThePersistentOutboxAsync()
    {
        await using ReliableOutboxFixture fixture = await ReliableOutboxFixture.CreateAsync();
        var command = new ReliableCommand(NewId.NextGuid(), FailFirstAttempt: false);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ReliableEventSnapshot[] events = fixture.Deliveries.For(command.CorrelationId);

        Assert.Equal("alpha", Assert.Single(events, item => item.Message.Text == "First").RoutingKey);
        Assert.Equal("beta", Assert.Single(events, item => item.Message.Text == "Second").RoutingKey);
    }

    public sealed record ReliableCommand(Guid CorrelationId, bool FailFirstAttempt) : CorrelatedBy<Guid>;

    public sealed record ReliableEvent(Guid CorrelationId, string Text) : CorrelatedBy<Guid>;

    public sealed record CreateReliableState(Guid CorrelationId, bool FailFirstAttempt) : CorrelatedBy<Guid>;

    public sealed record StateVerified(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ExpectedConsumerFailure : Exception;

    public sealed class ExpectedSagaFailure : Exception;

    public sealed class ExpectedTransportSendFailure : Exception;

    public sealed class ReliableCommandConsumer(
        IPublishEndpoint publishEndpoint,
        ConsumerAttemptProbe attempts) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            int attempt = attempts.Increment(context.Message.CorrelationId);
            await context.Advanced().PublishAsync(
                new ReliableEvent(context.Message.CorrelationId, "First"),
                send => send.SetRoutingKey("alpha"),
                context.CancellationToken);
            await publishEndpoint.PublishAsync(
                new ReliableEvent(context.Message.CorrelationId, "Second"),
                send => send.SetRoutingKey("beta"),
                context.CancellationToken);

            if (context.Message.FailFirstAttempt && attempt == 1)
                throw new ExpectedConsumerFailure();
        }
    }

    public sealed class ReliableEventConsumer(ReliableEventDeliveryProbe deliveries) : IConsumer<ReliableEvent>
    {
        public Task ConsumeAsync(ConsumeContext<ReliableEvent> context)
        {
            deliveries.Record(new ReliableEventSnapshot(context.Message, context.Advanced().RoutingKey()));
            return Task.CompletedTask;
        }
    }

    public sealed class ReliableState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ReliableStateMachine : ViciOneServiceBusStateMachine<ReliableState>
    {
        public const string VerifiedStateName = "Verified";

        public ReliableStateMachine(SagaAttemptProbe attempts)
        {
            InstanceState(state => state.CurrentState);
            Event(() => CreateState, configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => VerifyState, configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(CreateState)
                    .Then(context => attempts.Increment(context.Message.CorrelationId))
                    .TransitionTo(Created)
                    .Send(
                        context => context.ReceiveContext.InputAddress,
                        context => new StateVerified(context.Saga.CorrelationId))
                    .If(
                        context => context.Message.FailFirstAttempt && attempts.For(context.Message.CorrelationId) == 1,
                        failure => failure.Then(_ => throw new ExpectedSagaFailure())));
            During(Created, When(VerifyState).TransitionTo(Verified));
        }

        public State Created { get; private set; } = null!;

        public State Verified { get; private set; } = null!;

        public Event<CreateReliableState> CreateState { get; private set; } = null!;

        public Event<StateVerified> VerifyState { get; private set; } = null!;
    }

    public sealed class ReliableStateMap : SagaClassMap<ReliableState>
    {
        protected override void Configure(EntityTypeBuilder<ReliableState> entity, ModelBuilder model)
        {
            entity.ToTable("ReliableStates");
            entity.Property(state => state.CurrentState).HasMaxLength(64);
        }
    }

    public sealed class ReliableOutboxDbContext(DbContextOptions<ReliableOutboxDbContext> options) : SagaDbContext(options)
    {
        public DbSet<ReliableState> States => Set<ReliableState>();

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new ReliableStateMap(); }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    private sealed class ReliableCommandConsumerDefinition : ConsumerDefinition<ReliableCommandConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ReliableCommandConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(1));
            endpointConfigurator.UseEntityFrameworkOutbox<ReliableOutboxDbContext>(context);
        }
    }

    private sealed class ReliableEventConsumerDefinition : ConsumerDefinition<ReliableEventConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ReliableEventConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            if (endpointConfigurator is not IInMemoryReceiveEndpointConfigurator inMemory)
                throw new InvalidOperationException("Reliable routing-key tests require the in-memory transport configurator.");

            inMemory.ConfigureConsumeTopology = false;
            inMemory.Bind<ReliableEvent>(ExchangeType.Direct, "alpha");
            inMemory.Bind<ReliableEvent>(ExchangeType.Direct, "beta");
        }
    }

    private sealed class ReliableStateDefinition : SagaDefinition<ReliableState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ReliableState> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(1));
            endpointConfigurator.UseEntityFrameworkOutbox<ReliableOutboxDbContext>(context);
        }
    }

    public abstract class AttemptProbe
    {
        private readonly Dictionary<Guid, int> _attempts = [];
        private readonly Lock _lock = new();

        public int For(Guid correlationId)
        {
            lock (_lock)
                return _attempts.GetValueOrDefault(correlationId);
        }

        public int Increment(Guid correlationId)
        {
            lock (_lock)
            {
                int attempt = _attempts.GetValueOrDefault(correlationId) + 1;
                _attempts[correlationId] = attempt;
                return attempt;
            }
        }
    }

    public sealed class ConsumerAttemptProbe : AttemptProbe;

    public sealed class SagaAttemptProbe : AttemptProbe;

    public sealed class OutboxDeliveryFailureProbe(bool enabled) : ISendObserver
    {
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (enabled && context.Message is SerializedMessageBody
                && Interlocked.Increment(ref _attemptCount) == 1)
            {
                throw new ExpectedTransportSendFailure();
            }

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
            => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    public sealed record ReliableEventSnapshot(ReliableEvent Message, string? RoutingKey);

    public sealed class ReliableEventDeliveryProbe
    {
        private readonly Channel<ReliableEvent> _delivered = Channel.CreateUnbounded<ReliableEvent>();
        private readonly List<ReliableEventSnapshot> _snapshot = [];
        private readonly Lock _lock = new();

        public ReliableEventSnapshot[] For(Guid correlationId)
        {
            lock (_lock)
                return _snapshot.Where(item => item.Message.CorrelationId == correlationId).ToArray();
        }

        public void Record(ReliableEventSnapshot delivered)
        {
            lock (_lock)
                _snapshot.Add(delivered);
            if (!_delivered.Writer.TryWrite(delivered.Message))
                throw new InvalidOperationException("The reliable-event observation channel rejected a delivery.");
        }

        public Task<ReliableEvent> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _delivered.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    private sealed class ReliableOutboxFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;
        private readonly ConnectHandle _deliveryFailureObserverHandle;

        private ReliableOutboxFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            ConnectHandle deliveryFailureObserverHandle,
            ConsumerAttemptProbe consumerAttempts,
            SagaAttemptProbe sagaAttempts,
            ReliableEventDeliveryProbe deliveries,
            OutboxDeliveryFailureProbe deliveryFailures,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            _database = database;
            _deliveryFailureObserverHandle = deliveryFailureObserverHandle;
            Services = services;
            Harness = harness;
            ConsumerAttempts = consumerAttempts;
            SagaAttempts = sagaAttempts;
            Deliveries = deliveries;
            DeliveryFailures = deliveryFailures;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public ConsumerAttemptProbe ConsumerAttempts { get; }

        public OutboxDeliveryFailureProbe DeliveryFailures { get; }

        public ReliableEventDeliveryProbe Deliveries { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public SagaAttemptProbe SagaAttempts { get; }

        public ServiceProvider Services { get; }

        public static async Task<ReliableOutboxFixture> CreateAsync(bool failFirstOutboxDelivery = false)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
                "reliable-transactional-outbox",
                cancellationToken);
            await using (var setup = new ReliableOutboxDbContext(
                             new DbContextOptionsBuilder<ReliableOutboxDbContext>()
                                 .UseNpgsql(database.ConnectionString)
                                 .Options))
            {
                if (!await setup.Database.EnsureCreatedAsync(cancellationToken))
                    throw new InvalidOperationException("The reliable-outbox test database was not created.");
            }

            var consumerAttempts = new ConsumerAttemptProbe();
            var sagaAttempts = new SagaAttemptProbe();
            var deliveries = new ReliableEventDeliveryProbe();
            var deliveryFailures = new OutboxDeliveryFailureProbe(failFirstOutboxDelivery);
            var services = new ServiceCollection();
            services.AddSingleton(consumerAttempts);
            services.AddSingleton(sagaAttempts);
            services.AddSingleton(deliveries);
            services.AddSingleton(deliveryFailures);
            services.AddDbContext<ReliableOutboxDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.ConfigureEntityFrameworkTransactionalStore<ReliableOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                });
                configuration.AddConsumer<ReliableCommandConsumer, ReliableCommandConsumerDefinition>();
                configuration.AddConsumer<ReliableEventConsumer, ReliableEventConsumerDefinition>();
                configuration.AddSagaStateMachine<ReliableStateMachine, ReliableState, ReliableStateDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgres();
                        repository.UseExistingDbContext<ReliableOutboxDbContext>();
                    });
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.Publish<ReliableEvent>(topology => topology.ExchangeType = ExchangeType.Direct);
                    bus.ConfigureEndpoints(context);
                });
            });

            ServiceProvider? provider = null;
            try
            {
                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
                ConnectHandle deliveryFailureObserverHandle = harness.Bus.ConnectSendObserver(deliveryFailures);
                return new ReliableOutboxFixture(
                    database,
                    provider,
                    harness,
                    deliveryFailureObserverHandle,
                    consumerAttempts,
                    sagaAttempts,
                    deliveries,
                    deliveryFailures,
                    operationTimeout,
                    cancellationToken);
            }
            catch
            {
                if (provider is not null)
                    await provider.DisposeAsync();
                await database.DisposeAsync();
                throw;
            }
        }

        public Task<IReceivedMessage<CreateReliableState>> ConsumedAsync(
            Guid correlationId,
            CancellationToken cancellationToken) => Harness.Consumed
            .SelectAsync<CreateReliableState>(
                context => context.Context.Message.CorrelationId == correlationId && context.Exception is null,
                cancellationToken)
            .FirstObservedAsync(cancellationToken: cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);

        public async Task<ReliableState> ReadStateAsync(Guid correlationId)
        {
            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReliableOutboxDbContext>();
            return await dbContext.States.AsNoTracking()
                .SingleAsync(state => state.CorrelationId == correlationId, CancellationToken);
        }

        public Task<IReceivedMessage<StateVerified>> VerifiedAsync(
            Guid correlationId,
            CancellationToken cancellationToken) => Harness.Consumed
            .SelectAsync<StateVerified>(
                context => context.Context.Message.CorrelationId == correlationId && context.Exception is null,
                cancellationToken)
            .FirstObservedAsync(cancellationToken: cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);

        public IReceivedMessage<StateVerified>[] VerifiedSnapshot(Guid correlationId)
        {
            using var snapshot = new CancellationTokenSource();
            snapshot.Cancel();
            return Harness.Consumed.Select<StateVerified>(snapshot.Token)
                .Where(message => message.Context.Message.CorrelationId == correlationId && message.Exception is null)
                .ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            _deliveryFailureObserverHandle.Dispose();
            await Harness.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
