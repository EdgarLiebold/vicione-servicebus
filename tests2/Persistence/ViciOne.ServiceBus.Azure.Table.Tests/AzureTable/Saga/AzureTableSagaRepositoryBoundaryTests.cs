namespace ViciOne.ServiceBus.Azure.Table.Tests.AzureTable.Saga;

using global::Azure;
using global::Azure.Data.Tables;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AzureTableSagaRepositoryBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "load-propagates-caller-cancellation-token-and-instance")]
    public async Task Load_PropagatesCallerCancellationWithTheExactTokenAndException()
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
            () => context.Load(correlationId));

        Assert.Same(expected, actual);
        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
        Assert.Equal(nameof(BoundarySaga), table.ObservedPartitionKey);
        Assert.Equal(correlationId.ToString("D"), table.ObservedRowKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-FACTORY", "null-client-from-factory-fails-before-repository-operation")]
    public async Task Repository_FailsBeforeNetworkUseWhenTheClientFactoryReturnsNull()
    {
        ISagaRepository<BoundarySaga> repository = AzureTableSagaRepository<BoundarySaga>.Create(() => null!);
        var loadRepository = Assert.IsAssignableFrom<ILoadSagaRepository<BoundarySaga>>(repository);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => loadRepository.Load(Guid.Parse("018cc251-f400-7000-8000-000000000302")));

        Assert.Equal("The Azure Table client factory returned null.", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "non-conflict-storage-failure-preserves-identity")]
    public async Task Insert_PropagatesANonConflictStorageFailureUnchanged()
    {
        var expected = new RequestFailedException(500, "test-owned service failure");
        var table = new FailingWriteTableClient(insertFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            CancellationToken.None);

        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() =>
            context.Insert(new BoundarySaga { CorrelationId = Guid.NewGuid() }));

        Assert.Same(expected, actual);
        Assert.Equal(500, actual.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "actual-conflict-returns-no-inserted-context")]
    public async Task Insert_ReturnsNoContextOnlyForAnActualStorageConflict()
    {
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000303");
        var table = new FailingWriteTableClient(insertFailure: new RequestFailedException(409, "test-owned conflict"));
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            CancellationToken.None,
            correlationId);

        SagaConsumeContext<BoundarySaga, BoundaryMessage> actual = await context.Insert(
            new BoundarySaga { CorrelationId = correlationId });

        Assert.Null(actual);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "write-propagates-caller-cancellation-token-and-instance")]
    public async Task Write_PropagatesCallerCancellationWithTheExactTokenAndException(WriteOperation operation)
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var expected = new OperationCanceledException("caller canceled", innerException: null, caller.Token);
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.Add(
            new BoundarySaga { CorrelationId = Guid.NewGuid() });
        var eTag = new SagaETag("W/\"test-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ExecuteWrite(context, sagaContext, operation));

        Assert.Same(expected, actual);
        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "non-requested-dependency-cancellation-remains-a-write-failure")]
    public async Task Write_DoesNotMisclassifyANonRequestedDependencyCancellationAsCallerCancellation(WriteOperation operation)
    {
        using var caller = new CancellationTokenSource();
        var expected = new OperationCanceledException("dependency canceled", innerException: null, caller.Token);
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.Add(
            new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000304") });
        var eTag = new SagaETag("W/\"test-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWrite(context, sagaContext, operation));

        Assert.False(caller.IsCancellationRequested);
        Assert.Same(expected, actual.InnerException);
        Assert.Equal(caller.Token, table.ObservedCancellationToken);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "stale-etag-is-a-causal-saga-write-failure")]
    public async Task Write_ClassifiesAStaleEtagAsASagaFailureWithTheOriginalStorageError(WriteOperation operation)
    {
        var expected = new RequestFailedException(412, "test-owned stale ETag");
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000305");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.Add(
            new BoundarySaga { CorrelationId = correlationId });
        var eTag = new SagaETag("W/\"stale-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWrite(context, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(412, ((RequestFailedException)actual.InnerException!).Status);
        Assert.Equal(new ETag(eTag.ETag), table.ObservedETag);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "missing-etag-fails-before-network-use")]
    public async Task Write_RejectsAMissingEtagBeforeCallingTheTableClient(WriteOperation operation)
    {
        var table = new FailingWriteTableClient();
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000306");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.Add(
            new BoundarySaga { CorrelationId = correlationId });

        SagaException actual = await Assert.ThrowsAsync<SagaException>(() =>
            ExecuteWrite(context, sagaContext, operation));

        Assert.IsType<PayloadNotFoundException>(actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(0, table.WriteCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-BOUNDARY", "repository-context-rejects-missing-runtime-dependencies")]
    public async Task RepositoryContext_RejectsMissingRuntimeDependenciesAndSaga()
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
        ArgumentNullException nullSaga = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Insert(null!));
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

    private static Task ExecuteWrite(
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context,
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext,
        WriteOperation operation) => operation switch
        {
            WriteOperation.Update => context.Update(sagaContext),
            WriteOperation.Delete => context.Delete(sagaContext),
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
            ConsumeContext<BoundaryMessage> proxy =
                DispatchProxy.Create<ConsumeContext<BoundaryMessage>, CorrelatedConsumeContextProxy>();
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

        public override Task<Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            return Task.FromException<Response>(insertFailure
                ?? throw new InvalidOperationException("No insert failure was configured."));
        }

        public override Task<Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            ObservedETag = ifMatch;
            return Task.FromException<Response>(updateFailure
                ?? throw new InvalidOperationException("No update failure was configured."));
        }

        public override Task<Response> DeleteEntityAsync(
            string partitionKey,
            string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            WriteCallCount++;
            ObservedCancellationToken = cancellationToken;
            ObservedETag = ifMatch;
            return Task.FromException<Response>(deleteFailure
                ?? throw new InvalidOperationException("No delete failure was configured."));
        }
    }
}
