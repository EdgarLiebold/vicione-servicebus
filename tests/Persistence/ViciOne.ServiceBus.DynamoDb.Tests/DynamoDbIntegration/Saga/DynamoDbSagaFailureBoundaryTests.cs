using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.Tests.DynamoDbIntegration.Saga;

public sealed class DynamoDbSagaFailureBoundaryTests
{
    [Theory]
    [InlineData("null-payload")]
    [InlineData("foreign-payload-id")]
    [InlineData("foreign-row-key")]
    [InlineData("version-mismatch")]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "corrupt-persisted-saga-never-masquerades-as-absent-or-valid")]
    public async Task CorruptPersistedSaga_FailsClosedAsync(string corruption)
    {
        Guid requestedId = Guid.NewGuid();
        Guid foreignId = Guid.NewGuid();
        var persisted = new TestSaga { CorrelationId = requestedId, Version = 7 };
        var row = new DynamoDbSaga
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
            case "foreign-payload-id":
                persisted.CorrelationId = foreignId;
                row.Properties = JsonSerializer.Serialize(persisted, ServiceBusMetadataJson.Options);
                break;
            case "foreign-row-key":
                row.CorrelationId = foreignId.ToString("D");
                break;
            case "version-mismatch":
                row.VersionNumber++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption), corruption, null);
        }

        IDynamoDBContext database = LoadedSagaContextProbe.Create(row);
        using var context = new DynamoDbDatabaseContext<TestSaga>(database, new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table"));

        SerializationException actual = await Assert.ThrowsAsync<SerializationException>(
            () => context.LoadAsync(requestedId, TestContext.Current.CancellationToken));

        Assert.Contains(nameof(TestSaga), actual.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "provider-failures-preserve-identity-and-update-version")]
    public async Task ProviderFailure_IsPreservedAndFailedUpdateRestoresVersionAsync()
    {
        var failure = new InvalidOperationException("provider failure");
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(failure);
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 7 };

        Exception load = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.LoadAsync(saga.CorrelationId, CancellationToken.None));
        Exception insert = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.InsertAsync(saga, CancellationToken.None));
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
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 11 };

        OperationCanceledException load = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.LoadAsync(saga.CorrelationId, cancellation.Token));
        OperationCanceledException insert = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.InsertAsync(saga, cancellation.Token));
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
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = int.MaxValue };

        await Assert.ThrowsAsync<OverflowException>(() => context.UpdateAsync(saga, CancellationToken.None));

        Assert.Equal(int.MaxValue, saga.Version);
        Assert.Empty(probe.CancellationTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "failed-insert-never-returns-null-context")]
    public async Task InsertFailure_IsLoggedAndRethrownInsteadOfReturningANullSagaContextAsync()
    {
        var failure = new InvalidOperationException("insert failed");
        var database = new FailingDatabaseContext(failure);
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), CancellationToken.None);
        var repository = new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(database, consumeContext, null!);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid() };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(saga, TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Equal(1, database.InsertCount);
    }

    public sealed class TestSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }
    }

    public sealed record TestMessage;

    private sealed class FailingDatabaseContext(Exception failure) : DatabaseContext<TestSaga>
    {
        public int InsertCount { get; private set; }

        public Task AddAsync(TestSaga instance, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException(); }
        public Task InsertAsync(TestSaga instance, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); InsertCount++;
            return Task.FromException(failure);
        }

        public Task<TestSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.DynamoDb.Tests.DynamoDbIntegration.Saga.DynamoDbSagaFailureBoundaryTests.TestSaga?>(cancellationToken); throw new NotSupportedException(); }
        public Task UpdateAsync(TestSaga instance, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException(); }
        public Task DeleteAsync(TestSaga instance, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException(); }
        public void Dispose()
        {
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
            TestConsumeContext<T> context = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy>();
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

    private interface TestConsumeContext<out T> :
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

        public IReadOnlyList<CancellationToken> CancellationTokens => _cancellationTokens;

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
                    return Task.FromException<DynamoDbSaga>(_failure);
                case "DeleteAsync":
                    CaptureCancellation(args);
                    return Task.FromException(_failure);
                case "Dispose":
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
        private DynamoDbSaga _row = null!;

        public static IDynamoDBContext Create(DynamoDbSaga row)
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
