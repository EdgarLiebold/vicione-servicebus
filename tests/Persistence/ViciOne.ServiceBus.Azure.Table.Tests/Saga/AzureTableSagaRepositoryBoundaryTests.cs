using System.Reflection;
using System.Runtime.ExceptionServices;
using global::Azure;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.Saga;

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
        var database = new AzureTableSagaStorageContext<BoundarySaga>(
            table,
            new FixedPartitionSagaKeyFormatter(nameof(BoundarySaga)));
        var context = new AzureTableSagaLoadContext<BoundarySaga>(database, caller.Token);
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
        ISagaRepository<BoundarySaga> repository = AzureTableSagaRepository.Create<BoundarySaga>(() => null!);
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
    [InlineData(WriteOperation.Save, DependencyCancellationToken.Caller)]
    [InlineData(WriteOperation.Update, DependencyCancellationToken.Default)]
    [InlineData(WriteOperation.Delete, DependencyCancellationToken.Default)]
    [InlineData(WriteOperation.Save, DependencyCancellationToken.Default)]
    [InlineData(WriteOperation.Update, DependencyCancellationToken.Linked)]
    [InlineData(WriteOperation.Delete, DependencyCancellationToken.Linked)]
    [InlineData(WriteOperation.Save, DependencyCancellationToken.Linked)]
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
        var table = new FailingWriteTableClient(insertFailure: expected, updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        var eTag = new AzureTableSagaETag("W/\"test-etag\"");
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
    [InlineData(WriteOperation.Save)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CANCELLATION", "non-requested-dependency-cancellation-remains-a-write-failure")]
    public async Task Write_DoesNotMisclassifyANonRequestedDependencyCancellationAsCallerCancellationAsync(WriteOperation operation)
    {
        using var caller = new CancellationTokenSource();
        var expected = new OperationCanceledException("dependency canceled", innerException: null, caller.Token);
        var table = new FailingWriteTableClient(insertFailure: expected, updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            caller.Token);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000304") }, TestContext.Current.CancellationToken);
        var eTag = new AzureTableSagaETag("W/\"test-etag\"");
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
        var eTag = new AzureTableSagaETag("W/\"stale-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        AzureTableSagaConcurrencyException actual = await Assert.ThrowsAsync<AzureTableSagaConcurrencyException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(412, ((RequestFailedException)actual.InnerException!).Status);
        Assert.Equal(new ETag(eTag.ETag), table.ObservedETag);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "duplicate-save-maps-to-typed-retryable-concurrency")]
    public async Task Save_MapsADuplicatePersistenceKeyToTypedConcurrencyWithTheOriginalStorageErrorAsync()
    {
        var expected = new RequestFailedException(409, "test-owned duplicate persistence key");
        var table = new FailingWriteTableClient(insertFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000309");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(
            new BoundarySaga { CorrelationId = correlationId },
            TestContext.Current.CancellationToken);

        AzureTableSagaConcurrencyException actual = await Assert.ThrowsAsync<AzureTableSagaConcurrencyException>(() =>
            context.SaveAsync(sagaContext, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(BoundarySaga), actual.SagaType);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(409, ((RequestFailedException)actual.InnerException!).Status);
    }

    [Theory]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "deleted-entity-maps-to-typed-retryable-concurrency")]
    public async Task Write_MapsADeletedEntityToTypedConcurrencyWithoutMisclassifyingAMissingTableAsync(WriteOperation operation)
    {
        var expected = new RequestFailedException(
            404,
            "test-owned missing entity",
            "ResourceNotFound",
            new InvalidOperationException("response cause"));
        var table = new FailingWriteTableClient(updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000310");
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(
            new BoundarySaga { CorrelationId = correlationId },
            TestContext.Current.CancellationToken);
        var eTag = new AzureTableSagaETag("W/\"deleted-etag\"");
        sagaContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        AzureTableSagaConcurrencyException actual = await Assert.ThrowsAsync<AzureTableSagaConcurrencyException>(() =>
            ExecuteWriteAsync(context, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal("ResourceNotFound", ((RequestFailedException)actual.InnerException!).ErrorCode);
    }

    [Theory]
    [InlineData(WriteOperation.Update, 400)]
    [InlineData(WriteOperation.Update, 500)]
    [InlineData(WriteOperation.Save, 400)]
    [InlineData(WriteOperation.Save, 500)]
    [InlineData(WriteOperation.Delete, 400)]
    [InlineData(WriteOperation.Delete, 500)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-CONCURRENCY", "non-concurrency-storage-errors-remain-unclassified-saga-failures")]
    public async Task Write_DoesNotPromoteOtherStorageFailuresToConcurrencyAsync(WriteOperation operation, int status)
    {
        var expected = new RequestFailedException(status, "test-owned non-concurrency failure");
        var table = new FailingWriteTableClient(insertFailure: expected, updateFailure: expected, deleteFailure: expected);
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context = CreateRepositoryContext(
            table,
            TestContext.Current.CancellationToken);
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000307") }, TestContext.Current.CancellationToken);
        var eTag = new AzureTableSagaETag("W/\"non-concurrency-etag\"");
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
        var database = new AzureTableSagaStorageContext<BoundarySaga>(
            table,
            new FixedPartitionSagaKeyFormatter(nameof(BoundarySaga)));
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new BoundaryMessage(),
            CancellationToken.None);
        var factory = new SagaConsumeContextFactory<IAzureTableSagaStorageContext<BoundarySaga>, BoundarySaga>();

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
        var saga = new BoundarySaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000308") };
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext = await context.AddAsync(
            saga,
            TestContext.Current.CancellationToken);

        Assert.Equal("instance", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.AddAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        ArgumentNullException nullSaga = await Assert.ThrowsAsync<ArgumentNullException>(() => context.InsertAsync(null!, TestContext.Current.CancellationToken));
        Assert.Equal("instance", nullSaga.ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.SaveAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.UpdateAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.DeleteAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.DiscardAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.UndoAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("consumeContext", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.CreateSagaConsumeContextAsync<BoundaryMessage>(null!, saga, SagaConsumeContextMode.Load))).ParamName);
        Assert.Equal("instance", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.CreateSagaConsumeContextAsync(consumeContext, null!, SagaConsumeContextMode.Load))).ParamName);

        ArgumentException emptyCorrelationId = await Assert.ThrowsAsync<ArgumentException>(() =>
            context.LoadAsync(Guid.Empty, TestContext.Current.CancellationToken));
        Assert.Equal("correlationId", emptyCorrelationId.ParamName);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.AddAsync(saga, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.InsertAsync(saga, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.LoadAsync(saga.CorrelationId, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.SaveAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.UpdateAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.DeleteAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.DiscardAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.UndoAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(0, table.WriteCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-BOUNDARY", "context-factory-validates-and-rejects-query-correlation")]
    public async Task ContextFactory_ValidatesInputsAndRejectsQueryCorrelationAsync()
    {
        var table = new FailingWriteTableClient();
        var provider = new FixedAzureTableClientProvider<BoundarySaga>(table);
        var consumeFactory = new SagaConsumeContextFactory<IAzureTableSagaStorageContext<BoundarySaga>, BoundarySaga>();
        var factory = new AzureTableSagaRepositoryContextFactory<BoundarySaga>(
            provider,
            consumeFactory,
            new FixedPartitionSagaKeyFormatter(nameof(BoundarySaga)));
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new BoundaryMessage(),
            CancellationToken.None);
        var query = new SagaQuery<BoundarySaga>(_ => true);
        IPipe<SagaRepositoryQueryContext<BoundarySaga, BoundaryMessage>> queryPipe =
            DispatchProxy.Create<IPipe<SagaRepositoryQueryContext<BoundarySaga, BoundaryMessage>>, UnsupportedInvocationProxy>();

        Assert.Equal("asyncMethod", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.ExecuteAsync<BoundarySaga>(null!, TestContext.Current.CancellationToken))).ParamName);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            factory.ExecuteAsync<BoundarySaga>(_ => Task.FromResult<BoundarySaga?>(null), canceled.Token))).CancellationToken);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendAsync<BoundaryMessage>(null!, null!))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendAsync(consumeContext, null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync<BoundaryMessage>(null!, query, queryPipe))).ParamName);
        Assert.Equal("query", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync(consumeContext, null!, queryPipe))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync(consumeContext, query, null!))).ParamName);

        NotSupportedException failure = await Assert.ThrowsAsync<NotSupportedException>(() =>
            factory.SendQueryAsync(consumeContext, query, queryPipe));
        Assert.Equal("Azure Table saga persistence does not support query correlation.", failure.Message);
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

        var database = new AzureTableSagaStorageContext<BoundarySaga>(
            table,
            new FixedPartitionSagaKeyFormatter(nameof(BoundarySaga)));
        return new AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage>(
            database,
            consumeContext,
            new SagaConsumeContextFactory<IAzureTableSagaStorageContext<BoundarySaga>, BoundarySaga>());
    }

    private static Task ExecuteWriteAsync(
        AzureTableSagaRepositoryContext<BoundarySaga, BoundaryMessage> context,
        SagaConsumeContext<BoundarySaga, BoundaryMessage> sagaContext,
        WriteOperation operation) => operation switch
        {
            WriteOperation.Update => context.UpdateAsync(sagaContext),
            WriteOperation.Delete => context.DeleteAsync(sagaContext),
            WriteOperation.Save => context.SaveAsync(sagaContext),
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
        Save,
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
        Exception? insertFailure = null,
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

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
