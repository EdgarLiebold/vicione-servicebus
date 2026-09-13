using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Saga;

public sealed class PostgreSqlSagaRepositoryIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-LIFECYCLE", "pessimistic-correlated-lifecycle-persists-on-serializable-transaction")]
    public async Task PessimisticRepository_PersistsTheCorrelatedLifecycleOnASerializableTransactionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-lifecycle",
            cancellationToken);
        await CreatePersistentSagaSchemaAsync(database.ConnectionString, cancellationToken);
        var transactions = new TransactionProbe();
        await using ServiceProvider provider = CreatePersistentSagaProvider(
            database.ConnectionString,
            timeout,
            transactions,
            configureRepository: repository =>
            {
                repository.UsePostgreSql();
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new StartPersistentSaga(sagaId, "created"), cancellationToken);
            IPublishedMessage<PersistentSagaStarted> started = await harness.Published
                .SelectAsync<PersistentSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new CompletePersistentSaga(sagaId, "completed"), cancellationToken);
            IPublishedMessage<PersistentSagaCompleted> completed = await harness.Published
                .SelectAsync<PersistentSagaCompleted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreatePersistentSagaDbContext(database.ConnectionString);
            PersistentSaga persisted = await verification.Sagas.AsNoTracking()
                .SingleAsync(saga => saga.CorrelationId == sagaId, cancellationToken);

            Assert.Equal("created", started.Context.Message.Value);
            Assert.Equal("completed", completed.Context.Message.Value);
            Assert.Equal("completed", persisted.Value);
            Assert.True(persisted.IsCompleted);
            IsolationLevel[] observed = transactions.Snapshot();
            Assert.True(observed.Length >= 2, $"Expected at least two saga transactions, observed {observed.Length}.");
            Assert.All(observed, isolation => Assert.Equal(IsolationLevel.Serializable, isolation));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-TRANSACTION", "optimistic-default-uses-read-committed-on-real-provider")]
    public async Task OptimisticRepository_DefaultsToAReadCommittedTransactionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-optimistic-transaction",
            cancellationToken);
        await CreatePersistentSagaSchemaAsync(database.ConnectionString, cancellationToken);
        var transactions = new TransactionProbe();
        await using ServiceProvider provider = CreatePersistentSagaProvider(
            database.ConnectionString,
            timeout,
            transactions,
            configureRepository: repository => repository.SetOptimisticConcurrency());
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new StartPersistentSaga(sagaId, "created"), cancellationToken);
            await harness.Published
                .SelectAsync<PersistentSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new CompletePersistentSaga(sagaId, "completed"), cancellationToken);
            await harness.Published
                .SelectAsync<PersistentSagaCompleted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreatePersistentSagaDbContext(database.ConnectionString);
            PersistentSaga persisted = await verification.Sagas.AsNoTracking()
                .SingleAsync(saga => saga.CorrelationId == sagaId, cancellationToken);
            IsolationLevel[] observed = transactions.Snapshot();

            Assert.True(persisted.IsCompleted);
            Assert.Equal("completed", persisted.Value);
            Assert.True(observed.Length >= 2, $"Expected at least two saga transactions, observed {observed.Length}.");
            Assert.All(observed, isolation => Assert.Equal(IsolationLevel.ReadCommitted, isolation));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-TRANSACTION", "optimistic-transaction-disable-reaches-real-provider")]
    public async Task OptimisticRepository_ExecutesWithoutATransactionWhenExplicitlyDisabledAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-no-transaction",
            cancellationToken);
        await CreatePersistentSagaSchemaAsync(database.ConnectionString, cancellationToken);
        var transactions = new TransactionProbe();
        await using ServiceProvider provider = CreatePersistentSagaProvider(
            database.ConnectionString,
            timeout,
            transactions,
            configureRepository: repository => repository.SetOptimisticConcurrency(useTransaction: false));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new StartPersistentSaga(sagaId, "created"), cancellationToken);
            await harness.Published
                .SelectAsync<PersistentSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new CompletePersistentSaga(sagaId, "completed"), cancellationToken);
            await harness.Published
                .SelectAsync<PersistentSagaCompleted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreatePersistentSagaDbContext(database.ConnectionString);
            PersistentSaga persisted = await verification.Sagas.AsNoTracking()
                .SingleAsync(saga => saga.CorrelationId == sagaId, cancellationToken);

            Assert.True(persisted.IsCompleted);
            Assert.Equal("completed", persisted.Value);
            Assert.Empty(transactions.Snapshot());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-TRANSACTION", "transactionless-optimistic-instances-remain-independent")]
    public async Task TransactionlessOptimisticRepository_PersistsIndependentSagaInstancesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-no-transaction-multiple",
            cancellationToken);
        await CreatePersistentSagaSchemaAsync(database.ConnectionString, cancellationToken);
        var transactions = new TransactionProbe();
        await using ServiceProvider provider = CreatePersistentSagaProvider(
            database.ConnectionString,
            timeout,
            transactions,
            configureRepository: repository => repository.SetOptimisticConcurrency(useTransaction: false));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid firstId = Guid.NewGuid();
            Guid secondId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);
            await Task.WhenAll(
                endpoint.SendAsync(new StartPersistentSaga(firstId, "first-created"), cancellationToken),
                endpoint.SendAsync(new StartPersistentSaga(secondId, "second-created"), cancellationToken));
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == secondId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await Task.WhenAll(
                endpoint.SendAsync(new CompletePersistentSaga(firstId, "first-completed"), cancellationToken),
                endpoint.SendAsync(new CompletePersistentSaga(secondId, "second-completed"), cancellationToken));
            await harness.Published.SelectAsync<PersistentSagaCompleted>(
                    observation => observation.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.Published.SelectAsync<PersistentSagaCompleted>(
                    observation => observation.Context.Message.CorrelationId == secondId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreatePersistentSagaDbContext(database.ConnectionString);
            PersistentSaga[] persisted = await verification.Sagas.AsNoTracking()
                .OrderBy(saga => saga.Value)
                .ToArrayAsync(cancellationToken);

            Assert.Collection(
                persisted,
                first =>
                {
                    Assert.Equal(firstId, first.CorrelationId);
                    Assert.Equal("first-completed", first.Value);
                    Assert.True(first.IsCompleted);
                },
                second =>
                {
                    Assert.Equal(secondId, second.CorrelationId);
                    Assert.Equal("second-completed", second.Value);
                    Assert.True(second.IsCompleted);
                });
            Assert.Empty(transactions.Snapshot());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static async Task CreatePersistentSagaSchemaAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var context = CreatePersistentSagaDbContext(connectionString);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static PersistentSagaDbContext CreatePersistentSagaDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<PersistentSagaDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static ServiceProvider CreatePersistentSagaProvider(
        string connectionString,
        TimeSpan timeout,
        TransactionProbe transactions,
        Action<IEntityFrameworkSagaRepositoryConfigurator<PersistentSaga>> configureRepository)
    {
        return new ServiceCollection()
            .AddSingleton(transactions)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<PersistentSaga, PersistentSagaDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        configureRepository(repository);
                        repository.AddDbContext<DbContext, PersistentSagaDbContext>((provider, builder) =>
                            builder.UseNpgsql(connectionString)
                                .AddInterceptors(provider.GetRequiredService<TransactionProbe>()));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
    }

    public sealed record StartPersistentSaga(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    public sealed record CompletePersistentSaga(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    public sealed record PersistentSagaStarted(Guid CorrelationId, string Value);

    public sealed record PersistentSagaCompleted(Guid CorrelationId, string Value);

    public sealed class PersistentSaga :
        ISaga,
        IInitiatedBy<StartPersistentSaga>,
        IOrchestrates<CompletePersistentSaga>
    {
        public Guid CorrelationId { get; set; }

        public bool IsCompleted { get; set; }

        public string Value { get; set; } = string.Empty;

        public Task ConsumeAsync(ConsumeContext<StartPersistentSaga> context)
        {
            Value = context.Message.Value;
            return context.Advanced().PublishAsync(new PersistentSagaStarted(CorrelationId, Value), context.CancellationToken);
        }

        public Task ConsumeAsync(ConsumeContext<CompletePersistentSaga> context)
        {
            Value = context.Message.Value;
            IsCompleted = true;
            return context.Advanced().PublishAsync(new PersistentSagaCompleted(CorrelationId, Value), context.CancellationToken);
        }
    }

    private sealed class PersistentSagaDefinition : SagaDefinition<PersistentSaga>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<PersistentSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }

    public sealed class PersistentSagaDbContext(DbContextOptions<PersistentSagaDbContext> options) : DbContext(options)
    {
        public DbSet<PersistentSaga> Sagas => Set<PersistentSaga>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersistentSaga>(entity =>
            {
                entity.ToTable("PersistentSagas");
                entity.HasKey(saga => saga.CorrelationId);
                entity.Property(saga => saga.Value).HasMaxLength(80);
            });
        }
    }

    public sealed class TransactionProbe : DbTransactionInterceptor
    {
        private readonly object _lock = new();
        private readonly List<IsolationLevel> _isolationLevels = [];

        public IsolationLevel[] Snapshot()
        {
            lock (_lock)
                return [.. _isolationLevels];
        }

        public override DbTransaction TransactionStarted(
            DbConnection connection,
            TransactionEndEventData eventData,
            DbTransaction result)
        {
            Record(result.IsolationLevel);
            return result;
        }

        public override ValueTask<DbTransaction> TransactionStartedAsync(
            DbConnection connection,
            TransactionEndEventData eventData,
            DbTransaction result,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::System.Data.Common.DbTransaction>(cancellationToken); Record(result.IsolationLevel);
            return ValueTask.FromResult(result);
        }

        private void Record(IsolationLevel isolationLevel)
        {
            lock (_lock)
                _isolationLevels.Add(isolationLevel);
        }
    }
}
