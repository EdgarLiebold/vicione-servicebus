using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Saga;

public sealed class PostgreSqlReadOnlySagaTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-READ-ONLY", "response-observes-state-without-persisting-handler-mutation")]
    public async Task ReadOnlyEvent_RespondsFromThePersistedStateWithoutSavingItsMutationAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-read-only",
            cancellationToken);
        await using (var setup = CreateDbContext(database.ConnectionString))
            await setup.Database.EnsureCreatedAsync(cancellationToken);

        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<ReadOnlyStateMachine, ReadOnlyState, ReadOnlyStateDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgres();
                        repository.AddDbContext<DbContext, ReadOnlySagaDbContext>((_, builder) =>
                            builder.UseNpgsql(database.ConnectionString));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            IRequestClient<StartReadOnlySaga> startClient = harness.GetRequestClient<StartReadOnlySaga>();
            Response<ReadOnlySagaStarted> started = await startClient.GetResponseAsync<ReadOnlySagaStarted>(
                new StartReadOnlySaga(sagaId),
                cancellationToken);
            IRequestClient<CheckReadOnlySaga> statusClient = harness.GetRequestClient<CheckReadOnlySaga>();

            Response<ReadOnlySagaStatus> first = await statusClient.GetResponseAsync<ReadOnlySagaStatus>(
                new CheckReadOnlySaga(sagaId),
                cancellationToken);
            Response<ReadOnlySagaStatus> second = await statusClient.GetResponseAsync<ReadOnlySagaStatus>(
                new CheckReadOnlySaga(sagaId),
                cancellationToken);

            await using var verification = CreateDbContext(database.ConnectionString);
            ReadOnlyState persisted = await verification.States.AsNoTracking()
                .SingleAsync(state => state.CorrelationId == sagaId, cancellationToken);
            Assert.Equal(sagaId, started.Message.CorrelationId);
            Assert.Equal("Started", first.Message.Status);
            Assert.Equal("Started", second.Message.Status);
            Assert.Equal("Started", persisted.Status);
            Assert.Equal(ReadOnlyStateMachine.RunningStateName, persisted.CurrentState);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ReadOnlySagaDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<ReadOnlySagaDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    public sealed record StartReadOnlySaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CheckReadOnlySaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ReadOnlySagaStarted(Guid CorrelationId);

    public sealed record ReadOnlySagaStatus(Guid CorrelationId, string Status);

    public sealed class ReadOnlyState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    public sealed class ReadOnlyStateMachine : ViciOneServiceBusStateMachine<ReadOnlyState>
    {
        public const string RunningStateName = "Running";

        public ReadOnlyStateMachine()
        {
            InstanceState(state => state.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => StatusRequested, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.ReadOnly = true;
            });

            Initially(
                When(Started)
                    .Then(context => context.Saga.Status = "Started")
                    .Respond(context => new ReadOnlySagaStarted(context.Saga.CorrelationId))
                    .TransitionTo(Running));
            During(Running,
                When(StatusRequested)
                    .Respond(context => new ReadOnlySagaStatus(context.Saga.CorrelationId, context.Saga.Status))
                    .Then(context => context.Saga.Status = "This mutation must not be persisted"));
        }

        public State Running { get; private set; } = null!;

        public Event<StartReadOnlySaga> Started { get; private set; } = null!;

        public Event<CheckReadOnlySaga> StatusRequested { get; private set; } = null!;
    }

    private sealed class ReadOnlyStateDefinition : SagaDefinition<ReadOnlyState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ReadOnlyState> sagaConfigurator,
            IRegistrationContext context) => sagaConfigurator.UseVolatileOutbox(context);
    }

    public sealed class ReadOnlySagaDbContext(DbContextOptions<ReadOnlySagaDbContext> options) : DbContext(options)
    {
        public DbSet<ReadOnlyState> States => Set<ReadOnlyState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadOnlyState>(entity =>
            {
                entity.ToTable("ReadOnlyStates");
                entity.HasKey(state => state.CorrelationId);
                entity.Property(state => state.CurrentState).HasMaxLength(64);
                entity.Property(state => state.Status).HasMaxLength(160);
            });
        }
    }
}
