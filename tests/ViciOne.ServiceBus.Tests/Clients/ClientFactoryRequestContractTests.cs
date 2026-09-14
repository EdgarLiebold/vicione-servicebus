using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ClientFactoryRequestContractTests
{
    private static readonly Uri ExplicitAddress = new("loopback://localhost/explicit-request");
    private static readonly Uri ResponseAddress = new("loopback://localhost/request-response");
    private static readonly Uri RoutedAddress = new("loopback://localhost/routed-request");

    [Theory]
    [InlineData(RequestShape.Message)]
    [InlineData(RequestShape.ExplicitMessage)]
    [InlineData(RequestShape.ConsumedMessage)]
    [InlineData(RequestShape.ConsumedExplicitMessage)]
    [InlineData(RequestShape.Values)]
    [InlineData(RequestShape.ExplicitValues)]
    [InlineData(RequestShape.ConsumedValues)]
    [InlineData(RequestShape.ConsumedExplicitValues)]
    [RequirementCoverage("REQ-VSB-REQUEST-FACTORY-FORWARDING", "every-client-factory-request-shape")]
    public async Task ClientFactory_CreateRequestPreservesEveryArgumentAndResolutionShapeAsync(RequestShape shape)
    {
        bool usesValues = shape is RequestShape.Values
            or RequestShape.ExplicitValues
            or RequestShape.ConsumedValues
            or RequestShape.ConsumedExplicitValues;
        bool usesConsumeContext = shape is RequestShape.ConsumedMessage
            or RequestShape.ConsumedExplicitMessage
            or RequestShape.ConsumedValues
            or RequestShape.ConsumedExplicitValues;
        bool usesExplicitAddress = shape is RequestShape.ExplicitMessage
            or RequestShape.ConsumedExplicitMessage
            or RequestShape.ExplicitValues
            or RequestShape.ConsumedExplicitValues;
        bool usesRoute = shape is RequestShape.Values or RequestShape.ConsumedValues;
        var timeout = new RequestTimeout(TimeSpan.FromSeconds(37));
        var timeProvider = new RecordingTimerTimeProvider();
        var context = new RecordingClientFactoryContext(timeProvider, usesRoute);
        await using var factory = new ClientFactory(context);
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, UnusedConsumeContextProxy>();
        var message = new FactoryRequest("typed");
        var values = new { Value = "initialized" };
        using var cancellation = new CancellationTokenSource();

        using RequestHandle<FactoryRequest> handle = shape switch
        {
            RequestShape.Message => factory.CreateRequest(message, timeout, cancellation.Token),
            RequestShape.ExplicitMessage => factory.CreateRequest(ExplicitAddress, message, timeout, cancellation.Token),
            RequestShape.ConsumedMessage => factory.CreateRequest(consumeContext, message, timeout, cancellation.Token),
            RequestShape.ConsumedExplicitMessage => factory.CreateRequest(consumeContext, ExplicitAddress, message, timeout, cancellation.Token),
            RequestShape.Values => factory.CreateRequest<FactoryRequest>(values, timeout, cancellation.Token),
            RequestShape.ExplicitValues => factory.CreateRequest<FactoryRequest>(ExplicitAddress, values, timeout, cancellation.Token),
            RequestShape.ConsumedValues => factory.CreateRequest<FactoryRequest>(consumeContext, values, timeout, cancellation.Token),
            RequestShape.ConsumedExplicitValues => factory.CreateRequest<FactoryRequest>(consumeContext, ExplicitAddress, values, timeout, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };
        Task<Response<FactoryResponse>> response = handle.GetResponseAsync<FactoryResponse>(
            cancellationToken: CancellationToken.None);

        FactoryRequest sent = await handle.Message;

        Assert.Equal(1, context.ResolutionCount);
        Assert.Equal(
            usesExplicitAddress ? ExplicitAddress : usesRoute ? RoutedAddress : null,
            context.DestinationAddress);
        Assert.Equal(usesConsumeContext ? consumeContext : null, context.ConsumeContext);
        Assert.Equal(usesValues ? context.Endpoint.InitializedMessage : message, sent);
        Assert.Equal(usesValues ? values : null, context.Endpoint.Values);
        Assert.Equal(usesValues ? null : message, context.Endpoint.TypedMessage);
        Assert.Equal(context.Endpoint.RequestId, context.Endpoint.SendContext!.RequestId);
        Assert.Equal(ResponseAddress, context.Endpoint.SendContext.ResponseAddress);
        Assert.Equal(timeout.Value, context.Endpoint.SendContext.TimeToLive);
        Assert.Equal(timeout.Value, timeProvider.DueTime);

        cancellation.Cancel();
        TaskCanceledException exception = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        await timeProvider.Timer.Disposed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, timeProvider.Timer.DisposeCount);
    }

    [Theory]
    [InlineData(false, ScopedRequestShape.Message)]
    [InlineData(false, ScopedRequestShape.ExplicitMessage)]
    [InlineData(false, ScopedRequestShape.Values)]
    [InlineData(false, ScopedRequestShape.ExplicitValues)]
    [InlineData(true, ScopedRequestShape.Message)]
    [InlineData(true, ScopedRequestShape.ExplicitMessage)]
    [InlineData(true, ScopedRequestShape.Values)]
    [InlineData(true, ScopedRequestShape.ExplicitValues)]
    [RequirementCoverage("REQ-VSB-REQUEST-FACTORY-FORWARDING", "every-scoped-factory-request-shape")]
    public async Task ScopedClientFactory_CreateRequestPreservesScopeAndArgumentsAsync(
        bool hasConsumeContext,
        ScopedRequestShape shape)
    {
        bool usesValues = shape is ScopedRequestShape.Values or ScopedRequestShape.ExplicitValues;
        bool usesExplicitAddress = shape is ScopedRequestShape.ExplicitMessage or ScopedRequestShape.ExplicitValues;
        bool usesRoute = !usesExplicitAddress;
        var timeout = new RequestTimeout(TimeSpan.FromSeconds(29));
        var timeProvider = new RecordingTimerTimeProvider();
        var context = new RecordingClientFactoryContext(timeProvider, usesRoute);
        await using var clientFactory = new ClientFactory(context);
        ConsumeContext? consumeContext = hasConsumeContext
            ? DispatchProxy.Create<ConsumeContext, UnusedConsumeContextProxy>()
            : null;
        IScopedClientFactory factory = new ScopedClientFactory(clientFactory, consumeContext);
        var message = new FactoryRequest("scoped-typed");
        var values = new { Value = "scoped-initialized" };
        using var cancellation = new CancellationTokenSource();

        using RequestHandle<FactoryRequest> handle = shape switch
        {
            ScopedRequestShape.Message => factory.CreateRequest(message, timeout, cancellation.Token),
            ScopedRequestShape.ExplicitMessage => factory.CreateRequest(ExplicitAddress, message, timeout, cancellation.Token),
            ScopedRequestShape.Values => factory.CreateRequest<FactoryRequest>(values, timeout, cancellation.Token),
            ScopedRequestShape.ExplicitValues => factory.CreateRequest<FactoryRequest>(ExplicitAddress, values, timeout, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };
        Task<Response<FactoryResponse>> response = handle.GetResponseAsync<FactoryResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        FactoryRequest sent = await handle.Message;

        Assert.Equal(1, context.ResolutionCount);
        Assert.Equal(usesExplicitAddress ? ExplicitAddress : RoutedAddress, context.DestinationAddress);
        Assert.Equal(consumeContext, context.ConsumeContext);
        Assert.Equal(usesValues ? context.Endpoint.InitializedMessage : message, sent);
        Assert.Equal(usesValues ? values : null, context.Endpoint.Values);
        Assert.Equal(usesValues ? null : message, context.Endpoint.TypedMessage);
        Assert.Equal(timeout.Value, timeProvider.DueTime);

        cancellation.Cancel();
        TaskCanceledException exception = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-REQUEST-FACTORY-FORWARDING", "scoped-client-resolution-preserves-scope-route-and-address")]
    public async Task ScopedClientFactory_CreateRequestClientPreservesScopeRouteAndAddressAsync(
        bool hasConsumeContext,
        bool usesExplicitAddress)
    {
        var context = new RecordingClientFactoryContext(new RecordingTimerTimeProvider(), mapRoute: true);
        await using var clientFactory = new ClientFactory(context);
        ConsumeContext? consumeContext = hasConsumeContext
            ? DispatchProxy.Create<ConsumeContext, UnusedConsumeContextProxy>()
            : null;
        IScopedClientFactory factory = new ScopedClientFactory(clientFactory, consumeContext);

        IRequestClient<FactoryRequest> client = usesExplicitAddress
            ? factory.CreateRequestClient<FactoryRequest>(ExplicitAddress)
            : factory.CreateRequestClient<FactoryRequest>();

        Assert.NotNull(client);
        Assert.Equal(1, context.ResolutionCount);
        Assert.Equal(usesExplicitAddress ? ExplicitAddress : RoutedAddress, context.DestinationAddress);
        Assert.Equal(consumeContext, context.ConsumeContext);
    }

    public enum RequestShape
    {
        Message,
        ExplicitMessage,
        ConsumedMessage,
        ConsumedExplicitMessage,
        Values,
        ExplicitValues,
        ConsumedValues,
        ConsumedExplicitValues,
    }

    public enum ScopedRequestShape
    {
        Message,
        ExplicitMessage,
        Values,
        ExplicitValues,
    }

    private sealed record FactoryRequest(string Value);

    private sealed record FactoryResponse(string Value);

    private sealed class RecordingClientFactoryContext : ClientFactoryContext
    {
        private int _resolutionCount;

        public RecordingClientFactoryContext(TimeProvider timeProvider, bool mapRoute)
        {
            TimeProvider = timeProvider;
            var routes = new MessageRouteTable();
            if (mapRoute)
                routes.Map<FactoryRequest>(RoutedAddress);

            MessageRoutes = routes;
        }

        public RecordingRequestSendEndpoint Endpoint { get; } = new();

        public int ResolutionCount => Volatile.Read(ref _resolutionCount);

        public ConsumeContext? ConsumeContext { get; private set; }

        public Uri? DestinationAddress { get; private set; }

        public RequestTimeout DefaultTimeout => RequestTimeout.Default;

        public TimeProvider TimeProvider { get; }

        public IMessageRouteTable MessageRoutes { get; }

        public Uri ResponseAddress => ClientFactoryRequestContractTests.ResponseAddress;

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class
        {
            RecordResolution(null, consumeContext);
            return CastEndpoint<T>();
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class
        {
            RecordResolution(destinationAddress, consumeContext);
            return CastEndpoint<T>();
        }

        private IRequestSendEndpoint<T> CastEndpoint<T>()
            where T : class
        {
            Assert.Equal(typeof(FactoryRequest), typeof(T));
            return (IRequestSendEndpoint<T>)(object)Endpoint;
        }

        private void RecordResolution(Uri? destinationAddress, ConsumeContext? consumeContext)
        {
            Interlocked.Increment(ref _resolutionCount);
            DestinationAddress = destinationAddress;
            ConsumeContext = consumeContext;
        }
    }

    private sealed class RecordingRequestSendEndpoint : IRequestSendEndpoint<FactoryRequest>
    {
        public FactoryRequest InitializedMessage { get; } = new("initialized-result");

        public Guid RequestId { get; private set; }

        public object? Values { get; private set; }

        public FactoryRequest? TypedMessage { get; private set; }

        public MessageSendContext<FactoryRequest>? SendContext { get; private set; }

        public async Task<FactoryRequest> SendAsync(
            Guid requestId,
            object values,
            IPipe<SendContext<FactoryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            RequestId = requestId;
            Values = values;
            var context = new MessageSendContext<FactoryRequest>(InitializedMessage, cancellationToken);
            await pipe.SendAsync(context);
            SendContext = context;
            return InitializedMessage;
        }

        public async Task SendAsync(
            Guid requestId,
            FactoryRequest message,
            IPipe<SendContext<FactoryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            RequestId = requestId;
            TypedMessage = message;
            var context = new MessageSendContext<FactoryRequest>(message, cancellationToken);
            await pipe.SendAsync(context);
            SendContext = context;
        }
    }

    private sealed class RecordingTimerTimeProvider : TimeProvider
    {
        public RecordingTimer Timer { get; } = new();

        public TimeSpan? DueTime { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            DueTime = dueTime;
            return Timer;
        }

        public sealed class RecordingTimer : ITimer
        {
            private readonly TaskCompletionSource _disposed =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _disposeCount;

            public int DisposeCount => Volatile.Read(ref _disposeCount);

            public Task Disposed => _disposed.Task;

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCount);
                _disposed.TrySetResult();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }

    private class UnusedConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The recording context must not read {targetMethod?.Name}.");
    }
}
