using System.Reflection;
using Azure;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class MessageSessionSagaRepositoryTests
{
    [Theory]
    [InlineData("stored", 1)]
    [InlineData("different", 0)]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-SAGA", "query-correlation-evaluates-the-current-session-state")]
    public async Task QueryCorrelation_EvaluatesOnlyTheSagaStoredByTheCurrentSessionAsync(
        string expectedName,
        int expectedCount)
    {
        var saga = new SessionSaga { CorrelationId = Guid.NewGuid(), Name = "stored" };
        var session = new TestMessageSessionContext(BinaryData.FromObjectAsJson(saga));
        ConsumeContext<QueryMessage> context = CreateContext(
            new QueryMessage(),
            session,
            TestContext.Current.CancellationToken);
        var factory = new MessageSessionSagaRepositoryContextFactory<SessionSaga>(
            new SagaConsumeContextFactory<MessageSessionContext, SessionSaga>());
        var next = new QueryContextCapturePipe();

        await factory.SendQueryAsync(
            context,
            new SagaQuery<SessionSaga>(candidate => candidate.Name == expectedName),
            next);

        SagaRepositoryQueryContext<SessionSaga, QueryMessage> queryContext = Assert.IsAssignableFrom<SagaRepositoryQueryContext<SessionSaga, QueryMessage>>(
            next.Context);
        Assert.Equal(expectedCount, queryContext.Count);
        Assert.Equal(expectedCount == 1 ? [saga.CorrelationId] : [], queryContext.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-SAGA", "query-correlation-forwards-context-cancellation")]
    public async Task QueryCorrelation_ForwardsContextCancellationToSessionStateAsync()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var session = new TestMessageSessionContext(BinaryData.FromObjectAsJson(
            new SessionSaga { CorrelationId = Guid.NewGuid(), Name = "stored" }));
        ConsumeContext<QueryMessage> context = CreateContext(new QueryMessage(), session, source.Token);
        var factory = new MessageSessionSagaRepositoryContextFactory<SessionSaga>(
            new SagaConsumeContextFactory<MessageSessionContext, SessionSaga>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.SendQueryAsync(
            context,
            new SagaQuery<SessionSaga>(_ => true),
            new QueryContextCapturePipe()));

        Assert.Equal(source.Token, session.LastCancellationToken);
    }

    [Theory]
    [InlineData(SessionStateWrite.Save)]
    [InlineData(SessionStateWrite.Update)]
    [InlineData(SessionStateWrite.Delete)]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-SAGA", "state-writes-forward-operation-cancellation")]
    public async Task StateWrites_ForwardTheOperationCancellationTokenAsync(SessionStateWrite operation)
    {
        var session = new TestMessageSessionContext(null);
        var saga = new SessionSaga { CorrelationId = Guid.NewGuid(), Name = "stored" };
        var context = new MessageSessionSagaRepositoryContext<SessionSaga, QueryMessage>(
            CreateContext(new QueryMessage(), session, TestContext.Current.CancellationToken),
            null!);
        SagaConsumeContext<SessionSaga> sagaContext = DispatchProxy.Create<SagaConsumeContext<SessionSaga>, SagaContextProxy>();
        ((SagaContextProxy)(object)sagaContext).Saga = saga;
        using var source = new CancellationTokenSource();

        await (operation switch
        {
            SessionStateWrite.Save => context.SaveAsync(sagaContext, source.Token),
            SessionStateWrite.Update => context.UpdateAsync(sagaContext, source.Token),
            SessionStateWrite.Delete => context.DeleteAsync(sagaContext, source.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        });

        Assert.Equal(source.Token, session.LastCancellationToken);
    }

    private static ConsumeContext<T> CreateContext<T>(
        T message,
        MessageSessionContext session,
        CancellationToken cancellationToken = default)
        where T : class
    {
        TestConsumeContext<T> context = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message, session, cancellationToken);
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private object _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;
        private MessageSessionContext _session = null!;

        public void Configure<T>(T message, MessageSessionContext session, CancellationToken cancellationToken)
            where T : class
        {
            _message = message;
            _session = session;
            _cancellationToken = cancellationToken;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).CancellationToken = cancellationToken;
            _serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_Message":
                    return _message;
                case "get_CancellationToken":
                    return _cancellationToken;
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "HasPayloadType":
                    return ((Type)args![0]!).IsInstanceOfType(_session);
                case "TryGetPayload":
                    {
                        bool found = targetMethod.GetGenericArguments()[0].IsInstanceOfType(_session);
                        args![0] = found ? _session : null;
                        return found;
                    }
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private static readonly IPublishEndpointProvider PublishEndpointProvider = new UnsupportedPublishEndpointProvider();

        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_CancellationToken" => CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class SagaContextProxy : DispatchProxy
    {
        public SessionSaga Saga { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Saga"
                ? Saga
                : throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class UnsupportedPublishEndpointProvider : IPublishEndpointProvider
    {
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class TestMessageSessionContext(BinaryData? state) : MessageSessionContext
    {
        private BinaryData? _state = state;

        public string SessionId => "test-session";

        public DateTimeOffset LockedUntilUtc => DateTimeOffset.MaxValue;

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_state);
        }

        public Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            _state = state;
            return Task.CompletedTask;
        }

        public Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class QueryContextCapturePipe : IPipe<SagaRepositoryQueryContext<SessionSaga, QueryMessage>>
    {
        public PipeContext? Context { get; private set; }

        public Task SendAsync(SagaRepositoryQueryContext<SessionSaga, QueryMessage> context)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    public sealed class SessionSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public required string Name { get; set; }
    }

    public sealed record QueryMessage;

    public enum SessionStateWrite
    {
        Save,
        Update,
        Delete,
    }
}
