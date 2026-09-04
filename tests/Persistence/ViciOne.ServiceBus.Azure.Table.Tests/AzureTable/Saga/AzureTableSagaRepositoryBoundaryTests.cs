using System.Reflection;
using System.Runtime.ExceptionServices;
using global::Azure;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.AzureTable.Saga;

public sealed class AzureTableSagaRepositoryBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "load-propagates-caller-cancellation-token-and-instance")]
    public async Task Load_PropagatesCallerCancellationWithTheExactTokenAndExceptionAsync()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var expected = new OperationCanceledException("caller canceled", innerException: null, caller.Token);
        var table = new FailingLoadTableClient(expected);
        var database = new AzureTableDatabaseContext<BoundarySaga>(
            table,
            new ConstPartitionSagaKeyFormatter<BoundarySaga>(nameof(BoundarySaga)));
        var context = new AzureTableLoadSagaRepositoryContext<BoundarySaga>(database, caller.Token);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000301");

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.LoadAsync(correlationId, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
        Assert.Equal(nameof(BoundarySaga), table.ObservedPartitionKey);
        Assert.Equal(correlationId.ToString("D"), table.ObservedRowKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-FACTORY", "null-client-from-factory-fails-before-repository-operation")]
    public async Task Repository_FailsBeforeNetworkUseWhenTheClientFactoryReturnsNullAsync()
    {
        ISagaRepository<BoundarySaga> repository = AzureTableSagaRepository<BoundarySaga>.Create(() => null!);
        var loadRepository = Assert.IsAssignableFrom<ILoadSagaRepository<BoundarySaga>>(repository);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => loadRepository.LoadAsync(Guid.Parse("018cc251-f400-7000-8000-000000000302"), TestContext.Current.CancellationToken));

        Assert.Equal("The Azure Table client factory returned null.", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "non-conflict-storage-failure-preserves-identity")]
    public async Task Insert_PropagatesANonConflictStorageFailureUnchangedAsync()
    {
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000307");
        var expected = new RequestFailedException(500, "test-owned service failure");
        var table = new FailingWriteTableClient(insertFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            CancellationToken.None,
            correlationId);

        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() =>
            context.InsertAsync(new BoundarySaga { CorrelationId = correlationId }, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(500, actual.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "actual-conflict-returns-no-inserted-context")]
    public async Task Insert_ReturnsNoContextOnlyForAnActualStorageConflictAsync()
    {
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000303");
        var table = new FailingWriteTableClient(insertFailure: new RequestFailedException(409, "test-owned conflict"));
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            CancellationToken.None,
            correlationId);

        SagaConsumeContext<BoundarySaga, BoundaryMessage>? actual = await context.InsertAsync(
            new BoundarySaga { CorrelationId = correlationId }, TestContext.Current.CancellationToken);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData(WriteOperation.Update, DependencyCancellationToken.Caller)]
    [InlineData(WriteOperation.Delete, DependencyCancellationToken.Caller)]
    [InlineData(WriteOperation.Update, DependencyCancellationToken.Default)]
    [InlineData(WriteOperation.Delete, DependencyCancellationToken.Default)]
    [InlineData(WriteOperation.Update, DependencyCancellationToken.Linked)]
    [InlineData(WriteOperation.Delete, DependencyCancellationToken.Linked)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "write-propagates-caller-cancellation-token-and-instance")]
    public async Task Write_PropagatesCallerCancellationWithTheExactTokenAndExceptionAsync(
        WriteOperation operation,
        DependencyCancellationToken dependencyToken)
    {
        using var caller = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(caller.Token);
        caller.Cancel();
        CancellationToken exceptionToken = dependencyToken switch
        {
            DependencyCancellationToken.Caller => caller.Token,
            DependencyCancellationToken.Default => default,
            DependencyCancellationToken.Linked => linked.Token,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyToken), dependencyToken, null),
        };
        var expected = new OperationCanceledException("caller canceled", innerException: null, exceptionToken);
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        var eTag = new SagaETag("W/\"test-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.Same(expected, actual);
        Assert.Equal(exceptionToken, actual.CancellationToken);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
        if (dependencyToken is not DependencyCancellationToken.Caller)
            Assert.NotEqual(caller.Token, actual.CancellationToken);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "non-requested-dependency-cancellation-remains-a-write-failure")]
    public async Task Write_DoesNotMisclassifyANonRequestedDependencyCancellationAsCallerCancellationAsync(WriteOperation operation)
    {
        using var caller = new CancellationTokenSource();
        var expected = new OperationCanceledException("dependency canceled", innerException: null, caller.Token);
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000304") }, TestContext.Current.CancellationToken);
        var eTag = new SagaETag("W/\"test-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.False(caller.IsCancellationRequested);
        Assert.Same(expected, actual.InnerException);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "stale-etag-maps-to-typed-retryable-concurrency")]
    public async Task Write_MapsAStaleEtagToTypedConcurrencyWithTheOriginalStorageErrorAsync(WriteOperation operation)
    {
        var expected = new RequestFailedException(412, "test-owned stale ETag");
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000305");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = correlationId }, TestContext.Current.CancellationToken);
        var eTag = new SagaETag("W/\"stale-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        ConcurrencyException actual = await Assert.ThrowsAsync<ConcurrencyException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(412, ((RequestFailedException)actual.InnerException!).Status);
        Assert.Equal(new ETag(eTag.ETag), table.ObservedETag);
    }

    [Theory]
    [InlineData(WriteOperation.Update, 400)]
    [InlineData(WriteOperation.Update, 500)]
    [InlineData(WriteOperation.Delete, 400)]
    [InlineData(WriteOperation.Delete, 500)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "non-concurrency-storage-errors-remain-unclassified-saga-failures")]
    public async Task Write_DoesNotPromoteOtherStorageFailuresToConcurrencyAsync(WriteOperation operation, int status)
    {
        var expected = new RequestFailedException(status, "test-owned non-concurrency failure");
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000307") }, TestContext.Current.CancellationToken);
        var eTag = new SagaETag("W/\"non-concurrency-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.IsNotType<ConcurrencyException>(actual);
        Assert.Same(expected, actual.InnerException);
        Assert.Equal(status, ((RequestFailedException)actual.InnerException!).Status);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "missing-etag-fails-before-network-use")]
    public async Task Write_RejectsAMissingEtagBeforeCallingTheTableClientAsync(WriteOperation operation)
    {
        var table = new FailingWriteTableClient();
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000306");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = correlationId }, TestContext.Current.CancellationToken);

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.IsType<PayloadNotFoundException>(actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(0, table.WriteCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-BOUNDARY", "repository-context-rejects-missing-runtime-dependencies")]
    public async Task RepositoryContext_RejectsMissingRuntimeDependenciesAndSagaAsync()
    {
        var table = new FailingWriteTableClient();
        var database = new AzureTableDatabaseContext<BoundarySaga>(
            table,
            new ConstPartitionSagaKeyFormatter<BoundarySaga>(nameof(BoundarySaga)));
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new BoundaryMessage(),
            CancellationToken.None);
        var factory = new SagaConsumeContextFactory<DatabaseContext<BoundarySaga>, BoundarySaga>();

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(null!, consumeContext, factory)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(database, null!, factory)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(database, consumeContext, null!)).ParamName);

        var context = new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(
            database,
            consumeContext,
            factory);
        ArgumentNullException nullSaga = await Assert.ThrowsAsync<ArgumentNullException>(() => context.InsertAsync(null!, TestContext.Current.CancellationToken));
        Assert.Equal("instance", nullSaga.ParamName);
    }

    private static AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> CreateRepositoryContext(
        TableClient table,
        CancellationToken cancellationToken,
        Guid? correlationId = null)
    {
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new BoundaryMessage(),
            cancellationToken);
        if (correlationId.HasValue)
            consumeContext = CorrelatedConsumeContextProxy.Create(consumeContext, correlationId.Value);

        var database = new AzureTableDatabaseContext<BoundarySaga>(
            table,
            new ConstPartitionSagaKeyFormatter<BoundarySaga>(nameof(BoundarySaga)));
        return new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(
            database,
            consumeContext,
            new SagaConsumeContextFactory<DatabaseContext<BoundarySaga>, BoundarySaga>());
    }

    private static Task ExecuteWriteAsync(
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context,
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext,
        WriteOperation operation) => operation switch
        {
            WriteOperation.Update => context.UpdateAsync(sagaContext),
            WriteOperation.Delete => context.DeleteAsync(sagaContext),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

    private class CorrelatedConsumeContextProxy : DispatchProxy
    {
        private Guid _correlationId;
        private ConsumeContext<BoundaryMessage> _inner = null!;

        public static ConsumeContext<BoundaryMessage> Create(
            ConsumeContext<BoundaryMessage> inner,
            Guid correlationId)
        {
            BoundaryConsumeContext proxy =
                DispatchProxy.Create<BoundaryConsumeContext, CorrelatedConsumeContextProxy>();
            var implementation = (CorrelatedConsumeContextProxy)(object)proxy;
            implementation._inner = inner;
            implementation._correlationId = correlationId;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_CorrelationId")
                return _correlationId;

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private interface BoundaryConsumeContext :
        ConsumeContext<BoundaryMessage>,
        ConsumeContext;

    public sealed class BoundarySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record BoundaryMessage;

    public enum WriteOperation
    {
        Update,
        Delete,
    }

    public enum DependencyCancellationToken
    {
        Caller,
        Default,
        Linked,
    }

    private sealed class FailingLoadTableClient(OperationCanceledException failure) : TableClient
    {
        public CancellationToken ObservedCancellationToken { get; private set; }

        public string? ObservedPartitionKey { get; private set; }

        public string? ObservedRowKey { get; private set; }

        public override Task<NullableResponse<T>> GetEntityIfExistsAsync<T>(
            string partitionKey,
            string rowKey,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            ObservedCancellationToken = cancellationToken;
            ObservedPartitionKey = partitionKey;
            ObservedRowKey = rowKey;
            return Task.FromException<NullableResponse<T>>(failure);
        }
    }

    private sealed class FailingWriteTableClient(
        RequestFailedException? insertFailure = null,
        Exception? updateFailure = null,
        Exception? deleteFailure = null) : TableClient
    {
        public CancellationToken ObservedCancellationToken { get; private set; }

        public ETag? ObservedETag { get; private set; }

        public int WriteCallCount { get; private set; }

        public override Task<global::Azure.Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            return Task.FromException<global::Azure.Response>(insertFailure
                ?? throw new InvalidOperationException("No insert failure was configured."));
        }

        public override Task<global::Azure.Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            ObservedETag = ifMatch;
            return Task.FromException<global::Azure.Response>(updateFailure
                ?? throw new InvalidOperationException("No update failure was configured."));
        }

        public override Task<global::Azure.Response> DeleteEntityAsync(
            string partitionKey,
            string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            ObservedETag = ifMatch;
            return Task.FromException<global::Azure.Response>(deleteFailure
                ?? throw new InvalidOperationException("No delete failure was configured."));
        }
    }
}
