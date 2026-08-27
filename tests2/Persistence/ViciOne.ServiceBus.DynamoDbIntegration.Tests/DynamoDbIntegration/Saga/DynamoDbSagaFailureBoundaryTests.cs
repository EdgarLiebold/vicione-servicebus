namespace ViciOne.ServiceBus.DynamoDbIntegration.Tests.DynamoDbIntegration.Saga;

using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ViciOne.ServiceBus.DynamoDbIntegration.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class DynamoDbSagaFailureBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "provider-failures-preserve-identity-and-update-version")]
    public async Task ProviderFailure_IsPreservedAndFailedUpdateRestoresVersion()
    {
        var failure = new InvalidOperationException("provider failure");
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(failure);
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 7 };

        Exception load = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Load(saga.CorrelationId, CancellationToken.None));
        Exception insert = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Insert(saga, CancellationToken.None));
        Exception update = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Update(saga, CancellationToken.None));
        Exception delete = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Delete(saga, CancellationToken.None));

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
    public async Task RequestedCancellation_IsForwardedAndPreservedAcrossEveryDatabaseOperation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = new OperationCanceledException("caller canceled", innerException: null, cancellation.Token);
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(failure);
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = 11 };

        OperationCanceledException load = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Load(saga.CorrelationId, cancellation.Token));
        OperationCanceledException insert = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Insert(saga, cancellation.Token));
        OperationCanceledException update = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Update(saga, cancellation.Token));
        OperationCanceledException delete = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Delete(saga, cancellation.Token));

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
    public async Task VersionOverflow_IsRejectedBeforeProviderWrite()
    {
        DynamoDbContextProbe probe = DynamoDbContextProbe.Create(new InvalidOperationException("must not be observed"));
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        using var context = new DynamoDbDatabaseContext<TestSaga>(probe.Context, options);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Version = int.MaxValue };

        await Assert.ThrowsAsync<OverflowException>(() => context.Update(saga, CancellationToken.None));

        Assert.Equal(int.MaxValue, saga.Version);
        Assert.Empty(probe.CancellationTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-FAILURE", "failed-insert-never-returns-null-context")]
    public async Task InsertFailure_IsLoggedAndRethrownInsteadOfReturningANullSagaContext()
    {
        var failure = new InvalidOperationException("insert failed");
        var database = new FailingDatabaseContext(failure);
        ConsumeContext<TestMessage> consumeContext = ConsumeContextProxy.Create(new TestMessage(), CancellationToken.None);
        var repository = new DynamoDbSagaRepositoryContext<TestSaga, TestMessage>(database, consumeContext, null!);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid() };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.Insert(saga));

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

        public Task Add(TestSaga instance, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task Insert(TestSaga instance, CancellationToken cancellationToken)
        {
            InsertCount++;
            return Task.FromException(failure);
        }

        public Task<TestSaga> Load(Guid correlationId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task Update(TestSaga instance, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task Delete(TestSaga instance, CancellationToken cancellationToken) => throw new NotSupportedException();

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
            ConsumeContext<T> context = DispatchProxy.Create<ConsumeContext<T>, ConsumeContextProxy>();
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
}
