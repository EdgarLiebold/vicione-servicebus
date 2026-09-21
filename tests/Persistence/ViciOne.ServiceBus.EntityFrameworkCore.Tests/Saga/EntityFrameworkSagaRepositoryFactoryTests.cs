using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Operations;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga;

public sealed class EntityFrameworkSagaRepositoryFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "delegate-factory-releases-owned-context")]
    public async Task DelegateFactory_ReleaseDisposesTheFactoryOwnedContextAsync()
    {
        var dbContext = new DisposalTrackingDbContext();
        var factory = new DelegateSagaDbContextFactory<FactorySaga>(() => dbContext);

        DbContext created = factory.CreateDbContext();
        await factory.ReleaseAsync(created);

        Assert.Same(dbContext, created);
        Assert.True(dbContext.DisposeAsyncCalled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "container-factory-preserves-container-owned-context")]
    public async Task ContainerFactory_ReleasePreservesTheContainerOwnedContextAsync()
    {
        await using var dbContext = new DisposalTrackingDbContext();
        var factory = new ContainerSagaDbContextFactory<DisposalTrackingDbContext, FactorySaga>(dbContext);

        DbContext created = factory.CreateDbContext();
        await factory.ReleaseAsync(created);

        Assert.Same(dbContext, created);
        Assert.False(dbContext.DisposeAsyncCalled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "probe-preserves-container-owned-context")]
    public void Probe_PreservesTheContainerOwnedContext()
    {
        using var dbContext = new ProbeDbContext(new DbContextOptionsBuilder<ProbeDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        var factory = new ContainerSagaDbContextFactory<ProbeDbContext, FactorySaga>(dbContext);
        var probe = new ProbeResultBuilderTestDriver(Guid.NewGuid(), CancellationToken.None, TimeProvider.System);

        CreateProbeFactory(factory).Probe(probe.Context);

        Assert.False(dbContext.DisposeCalled);
        Assert.False(dbContext.DisposeAsyncCalled);
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(FactorySaga)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "probe-releases-delegate-owned-context")]
    public void Probe_ReleasesTheDelegateOwnedContextThroughItsFactory()
    {
        var dbContext = new ProbeDbContext(new DbContextOptionsBuilder<ProbeDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        var factory = new DelegateSagaDbContextFactory<FactorySaga>(() => dbContext);
        var probe = new ProbeResultBuilderTestDriver(Guid.NewGuid(), CancellationToken.None, TimeProvider.System);

        CreateProbeFactory(factory).Probe(probe.Context);

        Assert.True(dbContext.DisposeAsyncCalled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "probe-releases-context-after-model-failure")]
    public void Probe_ReleasesTheContextWhenModelConstructionFails()
    {
        var modelFailure = new InvalidOperationException("Model construction failed.");
        var dbContext = new FaultingProbeDbContext(new DbContextOptionsBuilder<FaultingProbeDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options, modelFailure);
        var factory = new DelegateSagaDbContextFactory<FactorySaga>(() => dbContext);
        var probe = new ProbeResultBuilderTestDriver(Guid.NewGuid(), CancellationToken.None, TimeProvider.System);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            CreateProbeFactory(factory).Probe(probe.Context));

        Assert.Same(modelFailure, actual);
        Assert.True(dbContext.DisposeAsyncCalled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "probe-propagates-release-failure")]
    public void Probe_PropagatesTheFactoryReleaseFailure()
    {
        var releaseFailure = new InvalidOperationException("Factory release failed.");
        using var dbContext = new ReleaseFaultingProbeDbContext(
            new DbContextOptionsBuilder<ReleaseFaultingProbeDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options,
            releaseFailure);
        var factory = new DelegateSagaDbContextFactory<FactorySaga>(() => dbContext);
        var probe = new ProbeResultBuilderTestDriver(Guid.NewGuid(), CancellationToken.None, TimeProvider.System);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            CreateProbeFactory(factory).Probe(probe.Context));

        Assert.Same(releaseFailure, actual);
        Assert.Equal(1, dbContext.DisposeAsyncCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-default-is-transactional")]
    public async Task CreateOptimistic_DefaultExecutesTheLoadInsideATransactionAsync()
    {
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>
            .CreateOptimistic(database.CreateContext);

        FactorySaga? loaded = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.VisibleSagaId, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(1, database.Observer.StartedCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-explicit-transaction-option-governs-execution")]
    public async Task CreateOptimistic_ExplicitTransactionOptionGovernsExecutionAsync(
        bool transactionEnabled,
        int expectedStartedTransactions)
    {
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreateOptimistic(
            database.CreateContext,
            isTransactionEnabled: transactionEnabled);

        FactorySaga? loaded = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.VisibleSagaId, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(expectedStartedTransactions, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-options-govern-execution")]
    public async Task CreateOptimistic_AppliesTransactionAndQueryOptionsToExecutionAsync()
    {
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreateOptimistic(
            database.CreateContext,
            query => query.Where(saga => saga.IsVisible),
            isTransactionEnabled: false);

        FactorySaga? visible = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.VisibleSagaId, TestContext.Current.CancellationToken);
        FactorySaga? hidden = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.HiddenSagaId, TestContext.Current.CancellationToken);
        Guid[] queryResults = (await ((IQuerySagaRepository<FactorySaga>)repository)
            .FindAsync(new SagaQuery<FactorySaga>(_ => true), TestContext.Current.CancellationToken))
            .ToArray();

        Assert.NotNull(visible);
        Assert.Null(hidden);
        Assert.Equal([database.VisibleSagaId], queryResults);
        Assert.Equal(0, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "pessimistic-provider-governs-execution")]
    public async Task CreatePessimistic_UsesTheSuppliedProviderInsideATransactionAsync()
    {
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync();
        var statementProvider = new RecordingLockStatementProvider();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreatePessimistic(
            database.CreateContext,
            statementProvider,
            query => query.Where(saga => saga.IsVisible));

        FactorySaga? loaded = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.VisibleSagaId, TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(1, statementProvider.RowLockStatementRequests);
        Assert.Equal(1, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "custom-interface-implementation-governs-load-and-query")]
    public async Task CustomExecutionStrategy_GovernsEveryDirectRepositoryOperationAsync()
    {
        var executionStrategy = new ExecutionStrategyProbe();
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync(executionStrategy);
        executionStrategy.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>
            .CreateOptimistic(database.CreateContext, isTransactionEnabled: false);

        FactorySaga? loaded = await ((ILoadSagaRepository<FactorySaga>)repository).LoadAsync(database.VisibleSagaId, TestContext.Current.CancellationToken);
        int executionsAfterLoad = executionStrategy.ExecutionCount;
        Guid[] queryResults = (await ((IQuerySagaRepository<FactorySaga>)repository)
            .FindAsync(new SagaQuery<FactorySaga>(_ => true), TestContext.Current.CancellationToken))
            .ToArray();

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(new[] { database.HiddenSagaId, database.VisibleSagaId }.Order(), queryResults.Order());
        Assert.Equal(2, executionsAfterLoad);
        Assert.Equal(4, executionStrategy.ExecutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "outer-transaction-query-avoids-additional-retry-boundary")]
    public async Task SendQuery_WithAnOuterTransaction_UsesOnlyTheProviderQueryExecutionAsync()
    {
        var executionStrategy = new ExecutionStrategyProbe();
        await using FactoryDatabase database = await FactoryDatabase.CreateAsync(executionStrategy);
        executionStrategy.Reset();
        var contextFactory = new EntityFrameworkSagaRepositoryContextFactory<FactorySaga>(
            new DelegateSagaDbContextFactory<FactorySaga>(database.CreateContext),
            new SagaConsumeContextFactory<DbContext, FactorySaga>(),
            new OptimisticSagaRepositoryLockStrategy<FactorySaga>(
                new OptimisticLoadQueryExecutor<FactorySaga>(),
                queryCustomization: null,
                System.Data.IsolationLevel.ReadCommitted,
                isTransactionEnabled: true));
        ConsumeContext<FactoryMessage> context = CreateConsumeContext(
            new FactoryMessage(),
            new TransactionPayload(Guid.Parse("9c49617d-5341-465f-b624-3807677f7b99")));
        int delivered = 0;

        await contextFactory.SendQueryAsync(
            context,
            new SagaQuery<FactorySaga>(_ => true),
            Pipe.Execute<ISagaRepositoryQueryContext<FactorySaga, FactoryMessage>>(queryContext =>
            {
                Assert.Equal(2, queryContext.Count);
                Interlocked.Increment(ref delivered);
            }));

        Assert.Equal(1, delivered);
        Assert.Equal(1, executionStrategy.ExecutionCount);
    }

    private static ConsumeContext<FactoryMessage> CreateConsumeContext(FactoryMessage message, IDbTransactionContext transaction)
    {
        FactoryConsumeContext context = DispatchProxy.Create<FactoryConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message, transaction, TestContext.Current.CancellationToken);
        return context;
    }

    private static EntityFrameworkSagaRepositoryContextFactory<FactorySaga> CreateProbeFactory(
        ISagaDbContextFactory<FactorySaga> dbContextFactory) => new(
        dbContextFactory,
        new SagaConsumeContextFactory<DbContext, FactorySaga>(),
        new OptimisticSagaRepositoryLockStrategy<FactorySaga>(
            new OptimisticLoadQueryExecutor<FactorySaga>(),
            queryCustomization: null,
            System.Data.IsolationLevel.ReadCommitted,
            isTransactionEnabled: true));

    private interface FactoryConsumeContext : ConsumeContext<FactoryMessage>, ConsumeContext;

    public sealed class FactorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public bool IsVisible { get; set; }
    }

    public sealed record FactoryMessage;

    private sealed record TransactionPayload(Guid TransactionId) : IDbTransactionContext;

    private sealed class FactoryDbContext(DbContextOptions<FactoryDbContext> options) : DbContext(options)
    {
        public DbSet<FactorySaga> Sagas => Set<FactorySaga>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactorySaga>(entity =>
            {
                entity.ToTable("FactorySagas");
                entity.HasKey(saga => saga.CorrelationId);
            });
        }
    }

    private sealed class DisposalTrackingDbContext : DbContext
    {
        public bool DisposeAsyncCalled { get; private set; }

        public override async ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            await base.DisposeAsync();
        }
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
    {
        public bool DisposeCalled { get; private set; }

        public bool DisposeAsyncCalled { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactorySaga>().HasKey(saga => saga.CorrelationId);
        }

        public override void Dispose()
        {
            DisposeCalled = true;
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return base.DisposeAsync();
        }
    }

    private sealed class FaultingProbeDbContext(
        DbContextOptions<FaultingProbeDbContext> options,
        Exception modelFailure) : DbContext(options)
    {
        public bool DisposeAsyncCalled { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            throw modelFailure;
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return base.DisposeAsync();
        }
    }

    private sealed class ReleaseFaultingProbeDbContext(
        DbContextOptions<ReleaseFaultingProbeDbContext> options,
        Exception releaseFailure) : DbContext(options)
    {
        public int DisposeAsyncCalls { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactorySaga>().HasKey(saga => saga.CorrelationId);
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalls++;
            return ValueTask.FromException(releaseFailure);
        }
    }

    private sealed class FactoryDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<FactoryDbContext> _options;

        private FactoryDatabase(SqliteConnection connection, DbContextOptions<FactoryDbContext> options,
            TransactionObserver observer, Guid visibleSagaId, Guid hiddenSagaId)
        {
            _connection = connection;
            _options = options;
            Observer = observer;
            VisibleSagaId = visibleSagaId;
            HiddenSagaId = hiddenSagaId;
        }

        public Guid HiddenSagaId { get; }
        public TransactionObserver Observer { get; }
        public Guid VisibleSagaId { get; }

        public static async Task<FactoryDatabase> CreateAsync(ExecutionStrategyProbe? executionStrategy = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var observer = new TransactionObserver();
            var optionsBuilder = new DbContextOptionsBuilder<FactoryDbContext>();
            optionsBuilder.UseSqlite(connection, sqlite =>
            {
                if (executionStrategy is not null)
                {
                    sqlite.ExecutionStrategy(dependencies =>
                        new RecordingExecutionStrategy(dependencies.CurrentContext.Context, executionStrategy));
                }
            });
            DbContextOptions<FactoryDbContext> options = optionsBuilder
                .AddInterceptors(observer)
                .Options;
            var visibleSagaId = Guid.NewGuid();
            var hiddenSagaId = Guid.NewGuid();
            await using (var context = new FactoryDbContext(options))
            {
                await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                context.Sagas.AddRange(
                    new FactorySaga { CorrelationId = visibleSagaId, IsVisible = true },
                    new FactorySaga { CorrelationId = hiddenSagaId, IsVisible = false });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            return new FactoryDatabase(connection, options, observer, visibleSagaId, hiddenSagaId);
        }

        public FactoryDbContext CreateContext() => new(_options);

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class TransactionObserver : DbTransactionInterceptor
    {
        private int _startedCount;

        public int StartedCount => Volatile.Read(ref _startedCount);

        public void Reset() => Volatile.Write(ref _startedCount, 0);

        public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            Interlocked.Increment(ref _startedCount);
            return result;
        }

        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData,
            DbTransaction result, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<DbTransaction>(cancellationToken);

            Interlocked.Increment(ref _startedCount);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingLockStatementProvider : ILockStatementProvider
    {
        private int _rowLockStatementRequests;

        public int RowLockStatementRequests => Volatile.Read(ref _rowLockStatementRequests);

        public string GetRowLockStatement<T>(DbContext context)
            where T : class
        {
            Interlocked.Increment(ref _rowLockStatementRequests);
            return "SELECT * FROM \"FactorySagas\" WHERE \"CorrelationId\" = @p0";
        }

        public string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class => throw new NotSupportedException();

        public string GetOutboxStatement(DbContext context) => throw new NotSupportedException();

        public string GetInboxCleanupLockStatement(DbContext context) => throw new NotSupportedException();
    }

    private sealed class ExecutionStrategyProbe
    {
        private int _executionCount;

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public void Record() => Interlocked.Increment(ref _executionCount);

        public void Reset() => Volatile.Write(ref _executionCount, 0);
    }

    private sealed class RecordingExecutionStrategy(DbContext context, ExecutionStrategyProbe probe) : IExecutionStrategy
    {
        public bool RetriesOnFailure => false;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        {
            probe.Record();
            return operation(context, state);
        }

        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            probe.Record();
            return operation(context, state, cancellationToken);
        }
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private FactoryMessage _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;
        private IDbTransactionContext _transaction = null!;

        public void Configure(FactoryMessage message, IDbTransactionContext transaction, CancellationToken cancellationToken)
        {
            _message = message;
            _transaction = transaction;
            _cancellationToken = cancellationToken;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).CancellationToken = cancellationToken;
            _serializerContext = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_CancellationToken":
                    return _cancellationToken;
                case "get_Message":
                    return _message;
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "HasPayloadType":
                    return ((Type)args![0]!).IsInstanceOfType(_transaction);
                case "TryGetPayload":
                    Type payloadType = targetMethod.GetGenericArguments()[0];
                    bool found = payloadType.IsInstanceOfType(_transaction);
                    args![0] = found ? _transaction : null;
                    return found;
                default:
                    throw new InvalidOperationException($"Unexpected consume-context member: {targetMethod?.Name ?? "<null>"}.");
            }
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private static readonly IPublishEndpointProvider PublishEndpointProvider = new NoopPublishEndpointProvider();

        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                _ => throw new InvalidOperationException($"Unexpected receive-context member: {targetMethod?.Name ?? "<null>"}."),
            };
        }
    }

    private class SerializerContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected serializer-context member: {targetMethod?.Name ?? "<null>"}.");
    }

    private sealed class NoopPublishEndpointProvider : IPublishEndpointProvider
    {
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<ISendEndpoint>(cancellationToken)
                : throw new NotSupportedException();
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }
}
