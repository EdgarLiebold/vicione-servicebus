using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.Tests.Saga;

public sealed class DynamoDbSagaFailureBoundaryTests
{
    [Theory]
    [InlineData("null-payload")]
    [InlineData("missing-payload")]
    [InlineData("empty-payload")]
    [InlineData("foreign-payload-id")]
    [InlineData("foreign-row-key")]
    [InlineData("foreign-entity-type")]
    [InlineData("version-mismatch")]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "corrupt-persisted-saga-never-masquerades-as-absent-or-valid")]
    public async Task CorruptPersistedSaga_FailsClosedAsync(string corruption)
    {
        Guid requestedId = Guid.NewGuid();
        Guid foreignId = Guid.NewGuid();
        var persisted = new TestSaga { CorrelationId = requestedId, Version = 7 };
        var row = new DynamoDbSagaDocument
        {
            CorrelationId = requestedId.ToString("D"),
            VersionNumber = persisted.Version,
            Properties = JsonSerializer.Serialize(persisted, ServiceBusMetadataJson.Options),
        };

        switch (corruption)
        {
            case "null-payload":
                row.Properties = "null";
                break;
            case "missing-payload":
                row.Properties = null!;
                break;
            case "empty-payload":
                row.Properties = "   ";
                break;
            case "foreign-payload-id":
                persisted.CorrelationId = foreignId;
                row.Properties = JsonSerializer.Serialize(persisted, ServiceBusMetadataJson.Options);
                break;
            case "foreign-row-key":
                row.CorrelationId = foreignId.ToString("D");
                break;
            case "foreign-entity-type":
                row.EntityType = "FOREIGN";
                break;
            case "version-mismatch":
                row.VersionNumber++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption), corruption, null);
        }

        IDynamoDBContext database = LoadedSagaContextProbe.Create(row);
        using var context = new DynamoDbSagaStore<TestSaga>(
            database,
            new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));

        SerializationException actual = await Assert.ThrowsAsync<SerializationException>(
            () => context.LoadAsync(requestedId, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(TestSaga), actual.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "null-saga-state-is-rejected-before-provider-use")]
    public async Task NullSagaState_IsRejectedBeforeProviderUseAsync()
    {
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(
            new InvalidOperationException("The provider must not be called."));
        using var context = new DynamoDbSagaStore<TestSaga>(
            probe.Context,
            new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.CreateAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.UpdateAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.DeleteAsync(null!, CancellationToken.None));

        Assert.Empty(probe.CancellationTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "provider-failures-preserve-identity-and-update-version")]
    public async Task ProviderFailure_IsPreservedAndFailedUpdateRestoresVersionAsync()
    {
        var failure = new InvalidOperationException("provider failure");
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(failure);
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbSagaStore<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 7 };

        Exception load = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LoadAsync(saga.CorrelationId, CancellationToken.None));
        Exception insert = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.CreateAsync(saga, CancellationToken.None));
        Exception update = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.UpdateAsync(saga, CancellationToken.None));
        Exception delete = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.DeleteAsync(saga, CancellationToken.None));

        Assert.Same(failure, load);
        Assert.Same(failure, insert);
        Assert.Same(failure, update);
        Assert.Same(failure, delete);
        Assert.Equal(7, saga.Version);
        Assert.Equal(4, probe.CancellationTokens.Count);
        Assert.All(probe.CancellationTokens, token => Assert.Equal(CancellationToken.None, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CANCELLATION", "requested-cancellation-is-forwarded-and-preserved")]
    public async Task RequestedCancellation_IsForwardedAndPreservedAcrossEveryDatabaseOperationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = new OperationCanceledException("caller canceled", innerException: null, cancellation.Token);
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(failure);
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbSagaStore<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 11 };

        OperationCanceledException load = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.LoadAsync(saga.CorrelationId, cancellation.Token));
        OperationCanceledException insert = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.CreateAsync(saga, cancellation.Token));
        OperationCanceledException update = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.UpdateAsync(saga, cancellation.Token));
        OperationCanceledException delete = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.DeleteAsync(saga, cancellation.Token));

        Assert.Same(failure, load);
        Assert.Same(failure, insert);
        Assert.Same(failure, update);
        Assert.Same(failure, delete);
        Assert.Equal(11, saga.Version);
        Assert.Equal(4, probe.CancellationTokens.Count);
        Assert.All(probe.CancellationTokens, token => Assert.Equal(cancellation.Token, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "version-overflow-is-rejected-before-provider-write")]
    public async Task VersionOverflow_IsRejectedBeforeProviderWriteAsync()
    {
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(new InvalidOperationException("must not be observed"));
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbSagaStore<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = int.MaxValue };

        await Assert.ThrowsAsync<OverflowException>(() => context.UpdateAsync(saga, CancellationToken.None));

        Assert.Equal(int.MaxValue, saga.Version);
        Assert.Empty(probe.CancellationTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-LIFETIME", "owned-provider-context-is-disposed-exactly-once")]
    public void SagaStore_DisposesOwnedProviderContextExactlyOnce()
    {
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(new InvalidOperationException("must not be observed"));
        var store = new DynamoDbSagaStore<TestSaga>(
            probe.Context,
            new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));

        store.Dispose();
        store.Dispose();

        Assert.Equal(1, probe.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "failed-insert-never-returns-null-context")]
    public async Task InsertFailure_IsLoggedAndRethrownInsteadOfReturningANullSagaContextAsync()
    {
        var failure = new InvalidOperationException("insert failed");
        var database = new FailingSagaStore(failure);
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), CancellationToken.None);
        ISagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga> factory =
            new SagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga>();
        var repository = new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(database, consumeContext, factory);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid() };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(saga, TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Equal(1, database.InsertCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "public-repository-loads-through-owned-operation-context")]
    public async Task PublicRepository_LoadsThroughOwnedOperationContextAsync()
    {
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000401");
        var persisted = new TestSaga { CorrelationId = correlationId, Version = 9 };
        var row = new DynamoDbSagaDocument
        {
            CorrelationId = correlationId.ToString("D"),
            VersionNumber = persisted.Version,
            Properties = JsonSerializer.Serialize(persisted, ServiceBusMetadataJson.Options),
        };
        IDynamoDBContext database = LoadedSagaContextProbe.Create(row);
        ILoadableSagaRepository<TestSaga> repository = DynamoDbSagaRepository.Create(
            () => database,
            new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));

        TestSaga actual = Assert.IsType<TestSaga>(
            await repository.LoadAsync(correlationId, TestContext.Current.CancellationToken));

        Assert.Equal(correlationId, actual.CorrelationId);
        Assert.Equal(9, actual.Version);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "repository-context-forwards-successful-operations-and-owns-store")]
    public async Task RepositoryContext_ForwardsSuccessfulOperationsAndOwnsStoreAsync()
    {
        using var lifetime = new CancellationTokenSource();
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), lifetime.Token);
        var store = new RecordingSagaStore();
        var factory = new SagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga>();
        var repository = new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(store, consumeContext, factory);
        var saga = new TestSaga
        {
            CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000402"),
            Version = 4,
        };
        store.LoadedSaga = saga;

        SagaConsumeContext<TestSaga, TestMessage> added = await repository.AddAsync(
            saga,
            TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> inserted = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(
            await repository.InsertAsync(saga, TestContext.Current.CancellationToken));
        SagaConsumeContext<TestSaga, TestMessage> loaded = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(
            await repository.LoadAsync(saga.CorrelationId, TestContext.Current.CancellationToken));
        await repository.SaveAsync(added, TestContext.Current.CancellationToken);
        await repository.UpdateAsync(added, TestContext.Current.CancellationToken);
        await repository.DeleteAsync(added, TestContext.Current.CancellationToken);
        await repository.DiscardAsync(added, TestContext.Current.CancellationToken);
        await repository.UndoAsync(added, TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> projected = await repository.CreateSagaConsumeContextAsync(
            consumeContext,
            saga,
            SagaConsumeContextMode.Load);
        repository.Dispose();

        Assert.Same(saga, added.Saga);
        Assert.Same(saga, inserted.Saga);
        Assert.Same(saga, loaded.Saga);
        Assert.Same(saga, projected.Saga);
        Assert.Equal(["create", "load", "create", "update", "delete"], store.Calls.Select(call => call.Operation));
        Assert.All(store.Calls, call => Assert.Equal(lifetime.Token, call.CancellationToken));
        Assert.Equal(1, store.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "repository-context-rejects-null-and-canceled-input-before-store-use")]
    public async Task RepositoryContext_RejectsNullAndCanceledInputBeforeStoreUseAsync()
    {
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), CancellationToken.None);
        var store = new RecordingSagaStore();
        var factory = new SagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga>();
        var repository = new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(store, consumeContext, factory);
        var saga = new TestSaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000403") };
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = await repository.AddAsync(
            saga,
            TestContext.Current.CancellationToken);

        Assert.Equal("store", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(null!, consumeContext, factory)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(store, null!, factory)).ParamName);
        Assert.Equal("consumeContextFactory", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(store, consumeContext, null!)).ParamName);
        Assert.Equal("instance", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.AddAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("instance", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.InsertAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.UpdateAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.DeleteAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.DiscardAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.UndoAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("consumeContext", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.CreateSagaConsumeContextAsync<TestMessage>(null!, saga, SagaConsumeContextMode.Load))).ParamName);
        Assert.Equal("instance", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.CreateSagaConsumeContextAsync(consumeContext, null!, SagaConsumeContextMode.Load))).ParamName);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.AddAsync(saga, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.InsertAsync(saga, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.LoadAsync(saga.CorrelationId, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.SaveAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.UpdateAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.DeleteAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.DiscardAsync(sagaContext, canceled.Token))).CancellationToken);
        Assert.Equal(canceled.Token, (await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.UndoAsync(sagaContext, canceled.Token))).CancellationToken);

        Assert.Empty(store.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "load-context-uses-lifetime-token-and-owns-store")]
    public async Task LoadContext_UsesLifetimeTokenAndOwnsStoreAsync()
    {
        using var lifetime = new CancellationTokenSource();
        var saga = new TestSaga { CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000404") };
        var store = new RecordingSagaStore { LoadedSaga = saga };
        var context = new DynamoDbSagaLoadContext<TestSaga>(store, lifetime.Token);

        TestSaga actual = Assert.IsType<TestSaga>(
            await context.LoadAsync(saga.CorrelationId, TestContext.Current.CancellationToken));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        OperationCanceledException cancellation = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.LoadAsync(saga.CorrelationId, canceled.Token));
        context.Dispose();

        Assert.Same(saga, actual);
        Assert.Equal(canceled.Token, cancellation.CancellationToken);
        (string operation, CancellationToken token) = Assert.Single(store.Calls);
        Assert.Equal("load", operation);
        Assert.Equal(lifetime.Token, token);
        Assert.Equal(1, store.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "typed-concurrency-errors-preserve-saga-identity-and-cause")]
    public void ConcurrencyException_ConstructorsPreserveSagaIdentityAndCause()
    {
        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000405");
        var cause = new ConditionalCheckFailedException("conditional write rejected");

        var withoutCause = new DynamoDbSagaConcurrencyException("conflict", typeof(TestSaga), correlationId);
        var withCause = new DynamoDbSagaConcurrencyException("conflict", typeof(TestSaga), correlationId, cause);

        Assert.Contains("conflict", withoutCause.Message, StringComparison.Ordinal);
        Assert.Equal(typeof(TestSaga), withoutCause.SagaType);
        Assert.Equal(correlationId, withoutCause.CorrelationId);
        Assert.Null(withoutCause.InnerException);
        Assert.Contains("conflict", withCause.Message, StringComparison.Ordinal);
        Assert.Equal(typeof(TestSaga), withCause.SagaType);
        Assert.Equal(correlationId, withCause.CorrelationId);
        Assert.Same(cause, withCause.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "context-factory-executes-sends-probes-and-disposes-operation-contexts")]
    public async Task ContextFactory_ExecutesSendsProbesAndDisposesOperationContextsAsync()
    {
        var providers = new List<DisposableDynamoDbContextProbe>();
        IDynamoDBContext CreateProvider()
        {
            DisposableDynamoDbContextProbe provider = DisposableDynamoDbContextProbe.Create();
            providers.Add(provider);
            return provider.Context;
        }

        var factory = new DynamoDbSagaRepositoryContextFactory<TestSaga>(
            new DynamoDbSagaContextFactory<TestSaga>(CreateProvider),
            new SagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga>(),
            new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));
        using var lifetime = new CancellationTokenSource();
        object expected = new();

        object? executed = await factory.ExecuteAsync<object>(context =>
        {
            Assert.Equal(lifetime.Token, context.CancellationToken);
            return Task.FromResult<object?>(expected);
        }, lifetime.Token);
        ISagaRepositoryContext<TestSaga, TestMessage>? observed = null;
        await factory.SendAsync(
            ConsumeContextProxy.Create(new TestMessage(), lifetime.Token),
            new RecordingPipe<ISagaRepositoryContext<TestSaga, TestMessage>>(context => observed = context));
        var probe = new RecordingProbeContext();
        factory.Probe(probe);

        Assert.Same(expected, executed);
        Assert.NotNull(observed);
        Assert.Equal(("persistence", "dynamodb"), probe.RecordedValue);
        Assert.Equal(2, providers.Count);
        Assert.All(providers, provider => Assert.Equal(1, provider.DisposeCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-BOUNDARY", "context-factory-validates-input-and-rejects-query-correlation")]
    public async Task ContextFactory_ValidatesInputAndRejectsQueryCorrelationAsync()
    {
        DisposableDynamoDbContextProbe provider = DisposableDynamoDbContextProbe.Create();
        var contextFactory = new DynamoDbSagaContextFactory<TestSaga>(() => provider.Context);
        var consumeFactory = new SagaConsumeContextFactory<IDynamoDbSagaStore<TestSaga>, TestSaga>();
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");

        Assert.Equal("contextFactory", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContextFactory<TestSaga>(null!, consumeFactory, options)).ParamName);
        Assert.Equal("consumeContextFactory", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContextFactory<TestSaga>(contextFactory, null!, options)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            new DynamoDbSagaRepositoryContextFactory<TestSaga>(contextFactory, consumeFactory, null!)).ParamName);

        var factory = new DynamoDbSagaRepositoryContextFactory<TestSaga>(contextFactory, consumeFactory, options);
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), CancellationToken.None);
        var query = new SagaQuery<TestSaga>(_ => true);
        IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>> queryPipe =
            DispatchProxy.Create<IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>>, UnsupportedInvocationProxy>();

        Assert.Equal("asyncMethod", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.ExecuteAsync<object>(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendAsync<TestMessage>(null!, null!))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendAsync(consumeContext, null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync<TestMessage>(null!, query, queryPipe))).ParamName);
        Assert.Equal("query", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync(consumeContext, null!, queryPipe))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.SendQueryAsync(consumeContext, query, null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => factory.Probe(null!)).ParamName);

        NotSupportedException failure = await Assert.ThrowsAsync<NotSupportedException>(() =>
            factory.SendQueryAsync(consumeContext, query, queryPipe));
        Assert.Equal("Amazon DynamoDB saga persistence does not support query correlation.", failure.Message);
        Assert.Equal(0, provider.DisposeCount);
    }

    public sealed class TestSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }
    }

    public sealed record TestMessage;

    private sealed class FailingSagaStore(Exception failure) : IDynamoDbSagaStore<TestSaga>
    {
        public int InsertCount { get; private set; }

        public Task CreateAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InsertCount++;
            return Task.FromException(failure);
        }

        public Task<TestSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task UpdateAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task DeleteAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingSagaStore : IDynamoDbSagaStore<TestSaga>
    {
        public List<(string Operation, CancellationToken CancellationToken)> Calls { get; } = [];

        public int DisposeCount { get; private set; }

        public TestSaga? LoadedSaga { get; set; }

        public Task CreateAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            Calls.Add(("create", cancellationToken));
            return Task.CompletedTask;
        }

        public Task<TestSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            Calls.Add(("load", cancellationToken));
            return Task.FromResult(LoadedSaga);
        }

        public Task UpdateAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            Calls.Add(("update", cancellationToken));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            Calls.Add(("delete", cancellationToken));
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class RecordingPipe<TContext>(Action<TContext> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context)
        {
            callback(context);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("recording");
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        public (string Key, string Value) RecordedValue { get; private set; }

        public void Add(string key, string? value)
        {
            RecordedValue = (key, value!);
        }

        public void Add(string key, object? value)
        {
            RecordedValue = (key, Assert.IsType<string>(value));
        }

        public void Set(object values) => throw new NotSupportedException();

        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();

        public ProbeContext CreateScope(string key) => this;
    }

    private class DisposableDynamoDbContextProbe : DispatchProxy
    {
        public IDynamoDBContext Context { get; private set; } = null!;

        public int DisposeCount { get; private set; }

        public static DisposableDynamoDbContextProbe Create()
        {
            IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, DisposableDynamoDbContextProbe>();
            var probe = (DisposableDynamoDbContextProbe)(object)context;
            probe.Context = context;
            return probe;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "Dispose")
            {
                DisposeCount++;
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private object _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;

        public static ConsumeContext<T> Create<T>(T message, CancellationToken cancellationToken)
            where T : class
        {
            ITestConsumeContext<T> context = DispatchProxy.Create<ITestConsumeContext<T>, ConsumeContextProxy>();
            var proxy = (ConsumeContextProxy)(object)context;
            proxy._message = message;
            proxy._cancellationToken = cancellationToken;
            proxy._receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)proxy._receiveContext).CancellationToken = cancellationToken;
            proxy._serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
            return context;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Message" => _message,
            "get_CancellationToken" => _cancellationToken,
            "get_CorrelationId" => null,
            "get_ReceiveContext" => _receiveContext,
            "get_SerializerContext" => _serializerContext,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private interface ITestConsumeContext<out T> :
        ConsumeContext<T>,
        ConsumeContext
        where T : class;

    private class ReceiveContextProxy : DispatchProxy
    {
        private readonly IPublishEndpointProvider _publishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnsupportedInvocationProxy>();

        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_PublishEndpointProvider" => _publishEndpointProvider,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class DynamoDbContextProbe : DispatchProxy
    {
        private readonly List<CancellationToken> _cancellationTokens = [];
        private Exception _failure = null!;
        private Table _table = null!;

        public IDynamoDBContext Context { get; private set; } = null!;

        public List<CancellationToken> CancellationTokens => _cancellationTokens;

        public int DisposeCount { get; private set; }

        public static DynamoDbContextProbe Create(Exception failure)
        {
            ArgumentNullException.ThrowIfNull(failure);

            IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, DynamoDbContextProbe>();
            var probe = (DynamoDbContextProbe)(object)context;
            probe.Context = context;
            probe._failure = failure;
            IAmazonDynamoDB client = DynamoDbClientProbe.Create(probe);
            probe._table = new TableBuilder(client, "valid-table")
                .AddHashKey("PK", DynamoDBEntryType.String)
                .AddRangeKey("SK", DynamoDBEntryType.String)
                .Build();
            return probe;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "GetTargetTable":
                    return _table;
                case "LoadAsync":
                    CaptureCancellation(args);
                    return Task.FromException<DynamoDbSagaDocument>(_failure);
                case "DeleteAsync":
                    CaptureCancellation(args);
                    return Task.FromException(_failure);
                case "Dispose":
                    DisposeCount++;
                    return null;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }

        private void CaptureCancellation(object?[]? args)
        {
            _cancellationTokens.Add(Assert.Single(args!.OfType<CancellationToken>()));
        }

        private class DynamoDbClientProbe : DispatchProxy
        {
            private DynamoDbContextProbe _owner = null!;

            public static IAmazonDynamoDB Create(DynamoDbContextProbe owner)
            {
                IAmazonDynamoDB client = DispatchProxy.Create<IAmazonDynamoDB, DynamoDbClientProbe>();
                ((DynamoDbClientProbe)(object)client)._owner = owner;
                return client;
            }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                ArgumentNullException.ThrowIfNull(targetMethod);

                switch (targetMethod.Name)
                {
                    case "get_Config":
                        return new AmazonDynamoDBConfig();
                    case "PutItemAsync":
                        _owner.CaptureCancellation(args);
                        return Task.FromException<PutItemResponse>(_owner._failure);
                    case "UpdateItemAsync":
                        _owner.CaptureCancellation(args);
                        return Task.FromException<UpdateItemResponse>(_owner._failure);
                    case "DeleteItemAsync":
                        _owner.CaptureCancellation(args);
                        return Task.FromException<DeleteItemResponse>(_owner._failure);
                    case "Dispose":
                        return null;
                    default:
                        throw new NotSupportedException(targetMethod.Name);
                }
            }
        }
    }

    private class LoadedSagaContextProbe : DispatchProxy
    {
        private DynamoDbSagaDocument _row = null!;

        public static IDynamoDBContext Create(DynamoDbSagaDocument row)
        {
            ArgumentNullException.ThrowIfNull(row);

            IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, LoadedSagaContextProbe>();
            ((LoadedSagaContextProbe)(object)context)._row = row;
            return context;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "LoadAsync" => Task.FromResult(_row),
            "Dispose" => null,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }
}
