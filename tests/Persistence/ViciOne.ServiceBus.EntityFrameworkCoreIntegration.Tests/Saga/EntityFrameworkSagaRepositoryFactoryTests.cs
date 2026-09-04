using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Saga;

public sealed class EntityFrameworkSagaRepositoryFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-default-is-transactional")]
    public async Task CreateOptimistic_DefaultExecutesTheLoadInsideATransaction()
    {
        await using FactoryDatabase database = await FactoryDatabase.Create();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>
            .CreateOptimistic(database.CreateContext);

        FactorySaga loaded = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.VisibleSagaId);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(1, database.Observer.StartedCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-explicit-transaction-option-governs-execution")]
    public async Task CreateOptimistic_ExplicitTransactionOptionGovernsExecution(
        bool transactionEnabled,
        int expectedStartedTransactions)
    {
        await using FactoryDatabase database = await FactoryDatabase.Create();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreateOptimistic(
            database.CreateContext,
            isTransactionEnabled: transactionEnabled);

        FactorySaga loaded = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.VisibleSagaId);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(expectedStartedTransactions, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "optimistic-options-govern-execution")]
    public async Task CreateOptimistic_AppliesTransactionAndQueryOptionsToExecution()
    {
        await using FactoryDatabase database = await FactoryDatabase.Create();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreateOptimistic(
            database.CreateContext,
            query => query.Where(saga => saga.IsVisible),
            isTransactionEnabled: false);

        FactorySaga visible = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.VisibleSagaId);
        FactorySaga hidden = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.HiddenSagaId);
        Guid[] queryResults = (await ((IQuerySagaRepository<FactorySaga>)repository)
            .Find(new SagaQuery<FactorySaga>(_ => true)))
            .ToArray();

        Assert.NotNull(visible);
        Assert.Null(hidden);
        Assert.Equal([database.VisibleSagaId], queryResults);
        Assert.Equal(0, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "pessimistic-provider-governs-execution")]
    public async Task CreatePessimistic_UsesTheSuppliedProviderInsideATransaction()
    {
        await using FactoryDatabase database = await FactoryDatabase.Create();
        var statementProvider = new RecordingLockStatementProvider();
        database.Observer.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>.CreatePessimistic(
            database.CreateContext,
            statementProvider,
            query => query.Where(saga => saga.IsVisible));

        FactorySaga loaded = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.VisibleSagaId);

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(1, statementProvider.RowLockStatementRequests);
        Assert.Equal(1, database.Observer.StartedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "custom-interface-implementation-governs-load-and-query")]
    public async Task CustomExecutionStrategy_GovernsEveryDirectRepositoryOperation()
    {
        var executionStrategy = new ExecutionStrategyProbe();
        await using FactoryDatabase database = await FactoryDatabase.Create(executionStrategy);
        executionStrategy.Reset();
        ISagaRepository<FactorySaga> repository = EntityFrameworkSagaRepository<FactorySaga>
            .CreateOptimistic(database.CreateContext, isTransactionEnabled: false);

        FactorySaga loaded = await ((ILoadSagaRepository<FactorySaga>)repository).Load(database.VisibleSagaId);
        int executionsAfterLoad = executionStrategy.ExecutionCount;
        Guid[] queryResults = (await ((IQuerySagaRepository<FactorySaga>)repository)
            .Find(new SagaQuery<FactorySaga>(_ => true)))
            .ToArray();

        Assert.NotNull(loaded);
        Assert.Equal(database.VisibleSagaId, loaded.CorrelationId);
        Assert.Equal(new[] { database.HiddenSagaId, database.VisibleSagaId }.Order(), queryResults.Order());
        Assert.Equal(2, executionsAfterLoad);
        Assert.Equal(4, executionStrategy.ExecutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "outer-transaction-query-avoids-additional-retry-boundary")]
    public async Task SendQuery_WithAnOuterTransaction_UsesOnlyTheProviderQueryExecution()
    {
        var executionStrategy = new ExecutionStrategyProbe();
        await using FactoryDatabase database = await FactoryDatabase.Create(executionStrategy);
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

        await contextFactory.SendQuery(
            context,
            new SagaQuery<FactorySaga>(_ => true),
            Pipe.Execute<SagaRepositoryQueryContext<FactorySaga, FactoryMessage>>(queryContext =>
            {
                Assert.Equal(2, queryContext.Count);
                Interlocked.Increment(ref delivered);
            }));

        Assert.Equal(1, delivered);
        Assert.Equal(1, executionStrategy.ExecutionCount);
    }

    private static ConsumeContext<FactoryMessage> CreateConsumeContext(FactoryMessage message, DbTransactionContext transaction)
    {
        ConsumeContext<FactoryMessage> context = DispatchProxy.Create<ConsumeContext<FactoryMessage>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message, transaction, TestContext.Current.CancellationToken);
        return context;
    }

    public sealed class FactorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public bool IsVisible { get; set; }
    }

    public sealed record FactoryMessage;

    private sealed record TransactionPayload(Guid TransactionId) : DbTransactionContext;

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

        public static async Task<FactoryDatabase> Create(ExecutionStrategyProbe? executionStrategy = null)
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
        private DbTransactionContext _transaction = null!;

        public void Configure(FactoryMessage message, DbTransactionContext transaction, CancellationToken cancellationToken)
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
        public Task<ISendEndpoint> GetPublishSendEndpoint<T>()
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }
}
