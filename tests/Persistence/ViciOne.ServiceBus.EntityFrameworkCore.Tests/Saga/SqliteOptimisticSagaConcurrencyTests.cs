using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga;

public sealed class SqliteOptimisticSagaConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SQLITE-OPTIMISTIC-CONCURRENCY", "stale-token-is-rejected")]
    public async Task ApplicationManagedToken_RejectsAStaleSagaUpdateAsync()
    {
        await using SqliteDatabase database = await SqliteDatabase.CreateAsync();
        Guid sagaId = Guid.NewGuid();
        await using (var seed = database.CreateContext())
        {
            seed.Sagas.Add(new OptimisticSaga { CorrelationId = sagaId, Value = 0 });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using OptimisticSagaDbContext firstContext = database.CreateContext();
        await using OptimisticSagaDbContext secondContext = database.CreateContext();
        var firstStrategy = CreateStrategy();
        var secondStrategy = CreateStrategy();
        OptimisticSaga first = Assert.IsType<OptimisticSaga>(await firstStrategy.LoadAsync(
            firstContext,
            sagaId,
            TestContext.Current.CancellationToken));
        OptimisticSaga second = Assert.IsType<OptimisticSaga>(await secondStrategy.LoadAsync(
            secondContext,
            sagaId,
            TestContext.Current.CancellationToken));
        Guid originalVersion = first.Version;
        first.Value = 1;
        second.Value = 2;

        await firstContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DbUpdateConcurrencyException conflict = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            secondContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Single(conflict.Entries);
        await using OptimisticSagaDbContext verification = database.CreateContext();
        OptimisticSaga stored = await verification.Sagas.AsNoTracking()
            .SingleAsync(saga => saga.CorrelationId == sagaId, TestContext.Current.CancellationToken);
        Assert.Equal(1, stored.Value);
        Assert.NotEqual(originalVersion, stored.Version);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SQLITE-OPTIMISTIC-CONCURRENCY", "retry-and-outbox-converge-once")]
    public async Task SagaRetry_AbsorbsOneConcurrencyConflictWithoutDuplicatePublicationAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteDatabase database = await SqliteDatabase.CreateAsync();
        var conflict = new FailFirstSagaUpdateInterceptor();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(conflict)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<OptimisticSaga, OptimisticSagaDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UseSqlite();
                        repository.AddDbContext<DbContext, OptimisticSagaDbContext>((serviceProvider, builder) =>
                            builder.UseSqlite(database.ConnectionString)
                                .AddInterceptors(serviceProvider.GetRequiredService<FailFirstSagaUpdateInterceptor>()));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<OptimisticSaga>(TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new StartOptimisticSaga(sagaId), cancellationToken);
            IPublishedMessage<OptimisticSagaStarted> started = await harness.Published
                .SelectAsync<OptimisticSagaStarted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new UpdateOptimisticSaga(sagaId), cancellationToken);
            IPublishedMessage<OptimisticSagaUpdated> updated = await harness.Published
                .SelectAsync<OptimisticSagaUpdated>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(sagaId, started.Context.Message.CorrelationId);
            Assert.Equal(sagaId, updated.Context.Message.CorrelationId);
            Assert.Equal(1, updated.Context.Message.Value);
            Assert.Equal(2, conflict.UpdateSaveAttempts);
            Assert.Single(harness.Published.Snapshot<OptimisticSagaUpdated>());
            await using OptimisticSagaDbContext verification = database.CreateContext();
            OptimisticSaga stored = await verification.Sagas.AsNoTracking()
                .SingleAsync(saga => saga.CorrelationId == sagaId, cancellationToken);
            Assert.Equal(1, stored.Value);
            Assert.NotEqual(Guid.Empty, stored.Version);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static OptimisticSagaRepositoryLockStrategy<OptimisticSaga> CreateStrategy()
    {
        var executor = new OptimisticLoadQueryExecutor<OptimisticSaga>();
        return new OptimisticSagaRepositoryLockStrategy<OptimisticSaga>(
            executor,
            queryCustomization: null,
            System.Data.IsolationLevel.Serializable,
            isTransactionEnabled: true);
    }

    public sealed record StartOptimisticSaga(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record UpdateOptimisticSaga(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record OptimisticSagaStarted(Guid CorrelationId);
    public sealed record OptimisticSagaUpdated(Guid CorrelationId, int Value);

    public sealed class OptimisticSaga :
        ISaga,
        IInitiatedBy<StartOptimisticSaga>,
        IOrchestrates<UpdateOptimisticSaga>
    {
        public Guid CorrelationId { get; set; }
        public int Value { get; set; }
        public Guid Version { get; set; }

        public Task ConsumeAsync(ConsumeContext<StartOptimisticSaga> context) =>
            context.Advanced().PublishAsync(new OptimisticSagaStarted(CorrelationId), context.CancellationToken);

        public Task ConsumeAsync(ConsumeContext<UpdateOptimisticSaga> context)
        {
            Value++;
            return context.Advanced().PublishAsync(new OptimisticSagaUpdated(CorrelationId, Value), context.CancellationToken);
        }
    }

    private sealed class OptimisticSagaDefinition : SagaDefinition<OptimisticSaga>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<OptimisticSaga> sagaConfigurator, IRegistrationContext context)
        {
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }

    public sealed class OptimisticSagaDbContext(DbContextOptions<OptimisticSagaDbContext> options) : DbContext(options)
    {
        public DbSet<OptimisticSaga> Sagas => Set<OptimisticSaga>();

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            RenewConcurrencyTokens();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            RenewConcurrencyTokens();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OptimisticSaga>(entity =>
            {
                entity.ToTable("OptimisticSagas");
                entity.HasKey(saga => saga.CorrelationId);
                entity.Property(saga => saga.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever();
            });
        }

        private void RenewConcurrencyTokens()
        {
            foreach (var entry in ChangeTracker.Entries<OptimisticSaga>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Property(saga => saga.Version).CurrentValue = Guid.NewGuid();
            }
        }
    }

    private sealed class FailFirstSagaUpdateInterceptor : SaveChangesInterceptor
    {
        private int _updateSaveAttempts;

        public int UpdateSaveAttempts => Volatile.Read(ref _updateSaveAttempts);

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ThrowFirstUpdate(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>>(cancellationToken); ThrowFirstUpdate(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private void ThrowFirstUpdate(DbContext? context)
        {
            if (context?.ChangeTracker.Entries<OptimisticSaga>().Any(entry => entry.State == EntityState.Modified) != true)
                return;

            if (Interlocked.Increment(ref _updateSaveAttempts) == 1)
                throw new DbUpdateConcurrencyException("Injected stale saga token for the retry contract.");
        }
    }

    private sealed class SqliteDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private SqliteDatabase(string databasePath, string connectionString)
        {
            _databasePath = databasePath;
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public static async Task<SqliteDatabase> CreateAsync()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"vicioneservicebus-ef-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={databasePath};Pooling=False";
            var database = new SqliteDatabase(databasePath, connectionString);
            await using OptimisticSagaDbContext context = database.CreateContext();
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return database;
        }

        public OptimisticSagaDbContext CreateContext()
        {
            DbContextOptions<OptimisticSagaDbContext> options = new DbContextOptionsBuilder<OptimisticSagaDbContext>()
                .UseSqlite(ConnectionString)
                .Options;
            return new OptimisticSagaDbContext(options);
        }

        public ValueTask DisposeAsync()
        {
            File.Delete(_databasePath);
            return ValueTask.CompletedTask;
        }
    }
}
