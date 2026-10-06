using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using System.Reflection;
using System.Runtime.ExceptionServices;
using global::Azure;
using global::Azure.Core;
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
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "insert-diagnostics-preserve-context-conflict-and-primary-failure")]
    public async Task Insert_DebugDiagnosticsPreserveStorageOutcomeAsync(int outcome, bool loggerThrows)
    {
        string template = outcome == 0
            ? "SAGA:{SagaType}:{CorrelationId} Used {MessageType}"
            : "SAGA:{SagaType}:{CorrelationId} Dupe {MessageType}";
        var diagnosticFailure = new IOException("Azure Table insert diagnostic failure");
        var storageFailure = new RequestFailedException(outcome == 1 ? 409 : 500, "test-owned insert storage failure");
        var logger = new InsertDiagnosticLogger(template, loggerThrows ? diagnosticFailure : null);
        using var messageLifetime = new CancellationTokenSource();
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000312");
        var successfulTable = new SuccessfulPreinsertTableClient();
        var failingTable = new FailingWriteTableClient(insertFailure: storageFailure);
        TableClient table = outcome == 0 ? successfulTable : failingTable;
        ILogContext? previous = LogContext.Current;
        Task<SagaConsumeContext<BoundarySaga, BoundaryMessage>?>? operation = null;
        SagaConsumeContext<BoundarySaga, BoundaryMessage>? inserted = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            ISagaRepositoryContext<BoundarySaga, BoundaryMessage> repository =
                CreateRepositoryContext(table, messageLifetime.Token, correlationId);
            operation = repository.InsertAsync(new BoundarySaga { CorrelationId = correlationId },
                TestContext.Current.CancellationToken);
            Exception? failure = await Record.ExceptionAsync(async () =>
            {
                inserted = await operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            });

            Assert.IsNotType<TimeoutException>(failure);
            Assert.True(operation.IsCompleted);
            Assert.False(messageLifetime.IsCancellationRequested);
            if (outcome == 0)
            {
                Assert.Equal(new[] { "insert" }, successfulTable.Calls);
                Assert.Equal(messageLifetime.Token, Assert.Single(successfulTable.Tokens));
                Assert.Equal(default(ETag), successfulTable.InputEtagBeforeResponse);
                TableEntity entity = Assert.IsType<TableEntity>(successfulTable.InsertedEntity);
                Assert.Equal(nameof(BoundarySaga), entity.PartitionKey);
                Assert.Equal(correlationId.ToString("D"), entity.RowKey);
                Assert.Equal(new ETag(SuccessfulPreinsertTableClient.ProviderEtag), entity.ETag);
            }
            else
            {
                Assert.Equal(1, failingTable.WriteCallCount);
                Assert.Equal(messageLifetime.Token, failingTable.ObservedCancellationToken);
            }
            InsertDiagnosticLogger.Entry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Equal(LogLevel.Debug, selected.Level);
            Assert.Equal(correlationId, selected.CorrelationId);
            if (outcome == 0)
                Assert.Null(selected.Error);
            else
                Assert.Same(storageFailure, selected.Error);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (loggerThrows)
            {
                Assert.Same(diagnosticFailure, logger.ThrownFailure);
                if (failure is not null && !ReferenceEquals(failure, storageFailure))
                    Assert.Same(diagnosticFailure, failure);
            }
            else
                Assert.Null(logger.ThrownFailure);

            if (outcome == 2)
            {
                Assert.Same(storageFailure, failure);
                Assert.Equal(500, Assert.IsType<RequestFailedException>(failure).Status);
                Assert.Null(inserted);
                Assert.True(operation.IsFaulted);
            }
            else
            {
                Assert.Null(failure);
                Assert.True(operation.IsCompletedSuccessfully);
                if (outcome == 0)
                {
                    SagaConsumeContext<BoundarySaga, BoundaryMessage> actual =
                        Assert.IsAssignableFrom<SagaConsumeContext<BoundarySaga, BoundaryMessage>>(inserted);
                    Assert.Equal(correlationId, actual.CorrelationId);
                    Assert.Equal(correlationId, actual.Saga.CorrelationId);
                    Assert.Equal(SuccessfulPreinsertTableClient.ProviderEtag, actual.GetPayload<AzureTableSagaETag>().ETag);
                    Assert.Equal(messageLifetime.Token, actual.CancellationToken);
                }
                else
                    Assert.Null(inserted);
            }
        }
        finally
        {
            try
            {
                if (operation is not null)
                    await ObserveInsertDiagnosticTaskAsync(operation);
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INSERT", "successful-preinsert-retains-provider-etag-for-conditional-writes")]
    public async Task SuccessfulInsert_RetainsResponseEtagForSubsequentConditionalWritesAsync()
    {
        using var lifetime = new CancellationTokenSource();
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000311");
        var table = new SuccessfulPreinsertTableClient();
        var repository = CreateRepositoryContext(table, lifetime.Token, correlationId);

        SagaConsumeContext<BoundarySaga, BoundaryMessage> inserted =
            Assert.IsAssignableFrom<SagaConsumeContext<BoundarySaga, BoundaryMessage>>(
                await repository.InsertAsync(new BoundarySaga { CorrelationId = correlationId }, TestContext.Current.CancellationToken));

        Assert.Equal(correlationId, inserted.Saga.CorrelationId);
        Assert.Equal(SuccessfulPreinsertTableClient.ProviderEtag, inserted.GetPayload<AzureTableSagaETag>().ETag);
        Assert.Equal(default(ETag), table.InputEtagBeforeResponse);
        Assert.Equal(nameof(BoundarySaga), table.InsertedEntity!.PartitionKey);
        Assert.Equal(correlationId.ToString("D"), table.InsertedEntity.RowKey);
        await repository.UpdateAsync(inserted, TestContext.Current.CancellationToken);
        await repository.DeleteAsync(inserted, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "insert", "update", "delete" }, table.Calls);
        Assert.All(table.Tokens, token => Assert.Equal(lifetime.Token, token));
        Assert.Equal(new ETag(SuccessfulPreinsertTableClient.ProviderEtag), table.UpdateEtag);
        Assert.Equal(table.UpdateEtag, table.DeleteEtag);
        Assert.Equal(TableUpdateMode.Replace, table.UpdateMode);
        Assert.Equal(table.InsertedEntity.PartitionKey, table.DeletedPartition);
        Assert.Equal(table.InsertedEntity.RowKey, table.DeletedRow);
    }

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
        IPipe<ISagaRepositoryQueryContext<BoundarySaga, BoundaryMessage>> queryPipe =
            DispatchProxy.Create<IPipe<ISagaRepositoryQueryContext<BoundarySaga, BoundaryMessage>>, UnsupportedInvocationProxy>();

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

    private static async Task ObserveInsertDiagnosticTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class InsertDiagnosticLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, Guid? CorrelationId, Exception? Error);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> values = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            string? template = values.FirstOrDefault(value => value.Key == "{OriginalFormat}").Value as string;
            object? correlation = values.FirstOrDefault(value => value.Key == "CorrelationId").Value;
            Entries.Add(new Entry(logLevel, template, correlation is Guid id ? id : null, exception));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
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

    private sealed class SuccessfulPreinsertTableClient : TableClient
    {
        public const string ProviderEtag = "W/\"provider-success-etag\"";
        public List<string> Calls { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public TableEntity? InsertedEntity { get; private set; }
        public ETag InputEtagBeforeResponse { get; private set; }
        public ETag UpdateEtag { get; private set; }
        public ETag DeleteEtag { get; private set; }
        public TableUpdateMode UpdateMode { get; private set; }
        public string? DeletedPartition { get; private set; }
        public string? DeletedRow { get; private set; }

        public override Task<global::Azure.Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            Calls.Add("insert");
            Tokens.Add(cancellationToken);
            InsertedEntity = Assert.IsType<TableEntity>(entity);
            InputEtagBeforeResponse = InsertedEntity.ETag;
            // SDK-compatible response-only token: never mutate the input entity.
            return Task.FromResult<global::Azure.Response>(new PreinsertResponse());
        }

        public override Task<global::Azure.Response> UpdateEntityAsync<T>(T entity, ETag ifMatch,
            TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken cancellationToken = default)
        {
            Calls.Add("update");
            Tokens.Add(cancellationToken);
            Assert.Equal(new ETag(ProviderEtag), Assert.IsType<TableEntity>(entity).ETag);
            UpdateEtag = ifMatch;
            UpdateMode = mode;
            return Task.FromResult<global::Azure.Response>(new PreinsertResponse());
        }

        public override Task<global::Azure.Response> DeleteEntityAsync(string partitionKey, string rowKey,
            ETag ifMatch = default, CancellationToken cancellationToken = default)
        {
            Calls.Add("delete");
            Tokens.Add(cancellationToken);
            DeletedPartition = partitionKey;
            DeletedRow = rowKey;
            DeleteEtag = ifMatch;
            return Task.FromResult<global::Azure.Response>(new PreinsertResponse());
        }
    }

    private sealed class PreinsertResponse : global::Azure.Response
    {
        public override int Status => 201;
        public override string ReasonPhrase => "Created";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "owned-preinsert-control";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => string.Equals(name, "ETag", StringComparison.OrdinalIgnoreCase);
        protected override IEnumerable<HttpHeader> EnumerateHeaders() =>
            [new HttpHeader("ETag", SuccessfulPreinsertTableClient.ProviderEtag)];
        protected override bool TryGetHeader(string name, out string value)
        {
            value = ContainsHeader(name) ? SuccessfulPreinsertTableClient.ProviderEtag : null!;
            return ContainsHeader(name);
        }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = ContainsHeader(name) ? [SuccessfulPreinsertTableClient.ProviderEtag] : null!;
            return ContainsHeader(name);
        }
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
