using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Contexts;
using ViciOne.ServiceBus.Clients.Endpoints;
using ViciOne.ServiceBus.Clients.Requests;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "factory-constructor-dependencies")]
    public void ClientFactories_RejectMissingConstructorDependenciesImmediately()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ClientFactory(null!)).ParamName);
        Assert.Equal("clientFactory", Assert.Throws<ArgumentNullException>(() => new ScopedClientFactory(null!, null)).ParamName);
        Assert.Equal("bus", Assert.Throws<ArgumentNullException>(() => new BusClientFactoryContext(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RESPONSE-ENDPOINT", "bus-context-forwards-response-connections")]
    public void BusContext_ForwardsBothResponseConnectionFormsWithoutRewritingTheirArguments()
    {
        IBus bus = DispatchProxy.Create<IBus, RecordingBusProxy>();
        var recorder = (RecordingBusProxy)(object)bus;
        var context = new BusClientFactoryContext(bus);
        IPipe<ConsumeContext<BoundaryResponse>> pipe = Pipe.Empty<ConsumeContext<BoundaryResponse>>();

        using ConnectHandle basicConnection = context.ConnectConsumePipe(pipe);
        using ConnectHandle configuredConnection = context.ConnectConsumePipe(pipe, ConnectPipeOptions.All);

        Assert.Equal(1, recorder.BasicConnectionCount);
        Assert.Equal(1, recorder.ConfiguredConnectionCount);
        Assert.Same(pipe, recorder.BasicPipe);
        Assert.Same(pipe, recorder.ConfiguredPipe);
        Assert.Equal(ConnectPipeOptions.All, recorder.Options);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-DIAGNOSTICS", "request-identity-and-contract")]
    public void RequestHandleProbe_ReportsItsIdentityAndRequestContract()
    {
        var context = new BoundaryClientFactoryContext();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback = async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new BoundaryRequest("unused");
        };
        using var handle = new ClientRequestHandle<BoundaryRequest>(context, callback);

        IProbeResult result = handle.GetProbeResult(TestContext.Current.CancellationToken);

        IReadOnlyDictionary<string, object> filter = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("filters", result.Results));
        Assert.Equal("request", Assert.Contains("filterType", filter));
        Assert.Equal(handle.RequestId, Assert.Contains("requestId", filter));
        Assert.Equal(TypeCache<BoundaryRequest>.ShortName, Assert.Contains("requestType", filter));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "response-registration-closes-before-send")]
    public void RequestHandle_RejectsResponseRegistrationAfterSendIsReleased()
    {
        var context = new BoundaryClientFactoryContext();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback = async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new BoundaryRequest("unused");
        };
        using var handle = new ClientRequestHandle<BoundaryRequest>(
            context,
            callback,
            TestContext.Current.CancellationToken);

        _ = handle.GetResponseAsync<BoundaryResponse>(
            readyToSend: true,
            TestContext.Current.CancellationToken);

        void RegisterAnotherResponse() => _ = handle.GetResponseAsync<AlternateResponse>(
            readyToSend: false,
            TestContext.Current.CancellationToken);

        RequestException exception = Assert.Throws<RequestException>(RegisterAnotherResponse);
        Assert.Contains("cannot be registered", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EntryPoint.CreateTyped, "message")]
    [InlineData(EntryPoint.CreateValues, "values")]
    [InlineData(EntryPoint.SingleTyped, "message")]
    [InlineData(EntryPoint.SingleValues, "values")]
    [InlineData(EntryPoint.DoubleTyped, "message")]
    [InlineData(EntryPoint.DoubleValues, "values")]
    [InlineData(EntryPoint.TripleTyped, "message")]
    [InlineData(EntryPoint.TripleValues, "values")]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "required-message-inputs")]
    public void EveryRequestEntryPoint_RejectsItsMissingMessageBeforeSending(
        EntryPoint entryPoint,
        string expectedParameter)
    {
        var endpoint = new RejectUnexpectedSendEndpoint();
        var client = new RequestClient<BoundaryRequest>(
            new BoundaryClientFactoryContext(),
            endpoint,
            new RequestTimeout(TimeSpan.FromSeconds(1)));

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Invoke(entryPoint, client));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Equal(0, endpoint.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "required-constructor-dependencies")]
    public void RequestClientAndHandle_RejectMissingConstructorDependenciesImmediately()
    {
        var context = new BoundaryClientFactoryContext();
        var endpoint = new RejectUnexpectedSendEndpoint();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback =
            (_, _, _) => Task.FromResult(new BoundaryRequest("unused"));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new RequestClient<BoundaryRequest>(null!, endpoint, RequestTimeout.Default)).ParamName);
        Assert.Equal("requestSendEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new RequestClient<BoundaryRequest>(context, null!, RequestTimeout.Default)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ClientRequestHandle<BoundaryRequest>(null!, callback)).ParamName);
        Assert.Equal("sendRequestCallback", Assert.Throws<ArgumentNullException>(() =>
            new ClientRequestHandle<BoundaryRequest>(context, null!)).ParamName);

        var responseHandle = new ResponseHandlerConnectHandle<BoundaryResponse>(
            new EmptyConnectHandle(),
            new TaskCompletionSource<ConsumeContext<BoundaryResponse>>(TaskCreationOptions.RunContinuationsAsynchronously),
            Task.CompletedTask);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            responseHandle.TrySetException(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "response-handler-forwards-both-release-forms")]
    public void ResponseHandler_ForwardsDisposalAndExplicitDisconnection()
    {
        var connection = new RecordingConnectHandle();
        var handle = new ResponseHandlerConnectHandle<BoundaryResponse>(
            connection,
            new TaskCompletionSource<ConsumeContext<BoundaryResponse>>(TaskCreationOptions.RunContinuationsAsynchronously),
            Task.CompletedTask);

        handle.Disconnect();
        handle.Dispose();

        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "response-forwards-exact-host-metadata")]
    public void MessageResponse_ForwardsTheExactHostMetadataInstance()
    {
        ConsumeContext<BoundaryResponse> context = DispatchProxy.Create<ConsumeContext<BoundaryResponse>, ResponseContextProxy>();
        var contextProxy = (ResponseContextProxy)(object)context;
        var message = new BoundaryResponse("response");
        var host = new global::ViciOne.ServiceBus.Metadata.BusHostInfo { MachineName = "response-host" };
        contextProxy.Message = message;
        contextProxy.Host = host;

        var response = new MessageResponse<BoundaryResponse>(context);

        Assert.Same(message, response.Message);
        Assert.Same(host, ((MessageContext)response).Host);
    }

    [Theory]
    [InlineData(ClientFactoryEntryPoint.Message, "message")]
    [InlineData(ClientFactoryEntryPoint.DestinationMessageAddress, "destinationAddress")]
    [InlineData(ClientFactoryEntryPoint.DestinationMessageMessage, "message")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedMessageContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedMessageMessage, "message")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationMessageContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationMessageAddress, "destinationAddress")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationMessageMessage, "message")]
    [InlineData(ClientFactoryEntryPoint.Values, "values")]
    [InlineData(ClientFactoryEntryPoint.DestinationValuesAddress, "destinationAddress")]
    [InlineData(ClientFactoryEntryPoint.DestinationValuesValues, "values")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedValuesContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedValuesValues, "values")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationValuesContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationValuesAddress, "destinationAddress")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationValuesValues, "values")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedClientContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.DestinationClientAddress, "destinationAddress")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationClientContext, "consumeContext")]
    [InlineData(ClientFactoryEntryPoint.CorrelatedDestinationClientAddress, "destinationAddress")]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "factory-validates-before-endpoint-resolution")]
    public void ClientFactory_RejectsEveryMissingRequiredInputBeforeEndpointResolution(
        ClientFactoryEntryPoint entryPoint,
        string expectedParameter)
    {
        var context = new BoundaryClientFactoryContext();
        var factory = new ClientFactory(context);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Invoke(entryPoint, factory));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Equal(0, context.ResolutionCount);
    }

    [Theory]
    [InlineData(ScopedFactoryEntryPoint.Message, "message")]
    [InlineData(ScopedFactoryEntryPoint.DestinationMessageAddress, "destinationAddress")]
    [InlineData(ScopedFactoryEntryPoint.DestinationMessageMessage, "message")]
    [InlineData(ScopedFactoryEntryPoint.Values, "values")]
    [InlineData(ScopedFactoryEntryPoint.DestinationValuesAddress, "destinationAddress")]
    [InlineData(ScopedFactoryEntryPoint.DestinationValuesValues, "values")]
    [InlineData(ScopedFactoryEntryPoint.DestinationClientAddress, "destinationAddress")]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "scoped-factory-validates-before-wrapped-factory")]
    public void ScopedClientFactory_RejectsEveryMissingRequiredInputBeforeWrappedFactoryUse(
        ScopedFactoryEntryPoint entryPoint,
        string expectedParameter)
    {
        var context = new BoundaryClientFactoryContext();
        var factory = new ScopedClientFactory(new ClientFactory(context), consumeContext: null);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Invoke(entryPoint, factory));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Equal(0, context.ResolutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "factory-contract-owns-one-asynchronous-disposal")]
    public async Task ClientFactory_ExposesOneIdempotentAsynchronousLifetimeAsync()
    {
        var context = new DisposableBoundaryClientFactoryContext();
        var factory = new ClientFactory(context);
        Task firstDisposal = factory.DisposeAsync().AsTask();
        await context.DisposalStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Task secondDisposal = factory.DisposeAsync().AsTask();

        try
        {
            Assert.Same(firstDisposal, secondDisposal);
            Assert.Equal(1, context.DisposeCount);
            Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IClientFactory)));

            ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(() =>
                factory.CreateRequestClient<BoundaryRequest>(default));
            Assert.Equal(nameof(ClientFactory), exception.ObjectName);
            Assert.Equal(0, context.ResolutionCount);
        }
        finally
        {
            context.ReleaseDisposal();
            await Task.WhenAll(firstDisposal, secondDisposal)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        Assert.Same(firstDisposal, factory.DisposeAsync().AsTask());
        Assert.Equal(1, context.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "factory-disposal-preserves-one-shared-failure")]
    public async Task ClientFactory_DisposalPreservesOneSharedContextFailureAsync()
    {
        var failure = new FactoryDisposalException();
        var context = new FaultingDisposableBoundaryClientFactoryContext(failure);
        var factory = new ClientFactory(context);

        Task firstDisposal = factory.DisposeAsync().AsTask();
        Task secondDisposal = factory.DisposeAsync().AsTask();
        FactoryDisposalException first = await Assert.ThrowsAsync<FactoryDisposalException>(() => firstDisposal);
        FactoryDisposalException second = await Assert.ThrowsAsync<FactoryDisposalException>(() => secondDisposal);

        Assert.Same(firstDisposal, secondDisposal);
        Assert.Same(failure, first);
        Assert.Same(first, second);
        Assert.Equal(1, context.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => factory.CreateRequestClient<BoundaryRequest>(default));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "pre-canceled-send-skips-endpoint-resolution")]
    public async Task PreCanceledRequestSend_SkipsEndpointResolutionAndPreservesTheTokenAsync()
    {
        var endpoint = new ResolutionProbeRequestSendEndpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => endpoint.SendAsync(
            Guid.NewGuid(),
            new BoundaryRequest("request"),
            Pipe.Empty<SendContext<BoundaryRequest>>(),
            cancellation.Token));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(0, endpoint.ResolutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "endpoint-resolution-receives-send-token")]
    public async Task RequestSend_ForwardsTheExactTokenToEndpointResolutionAsync()
    {
        var endpoint = new ResolutionProbeRequestSendEndpoint();
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAsync<ResolutionProbeException>(() => endpoint.SendAsync(
            Guid.NewGuid(),
            new BoundaryRequest("request"),
            Pipe.Empty<SendContext<BoundaryRequest>>(),
            cancellation.Token));

        Assert.Equal(1, endpoint.ResolutionCount);
        Assert.Equal(cancellation.Token, endpoint.ResolutionCancellationToken);
    }

    private static void Invoke(EntryPoint entryPoint, IRequestClient<BoundaryRequest> client)
    {
        switch (entryPoint)
        {
            case EntryPoint.CreateTyped:
                _ = client.Advanced().Create((BoundaryRequest)null!);
                break;
            case EntryPoint.CreateValues:
                _ = client.Advanced().Create((object)null!);
                break;
            case EntryPoint.SingleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.SingleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse>((object)null!);
                break;
            case EntryPoint.DoubleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.DoubleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse>((object)null!);
                break;
            case EntryPoint.TripleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse, ThirdResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.TripleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse, ThirdResponse>((object)null!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entryPoint), entryPoint, null);
        }
    }

    private static void Invoke(ClientFactoryEntryPoint entryPoint, IClientFactory factory)
    {
        var message = new BoundaryRequest("valid");
        var values = new { Value = "valid" };
        Uri destination = new("loopback://localhost/request");
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, UnusedConsumeContextProxy>();

        object result = entryPoint switch
        {
            ClientFactoryEntryPoint.Message => factory.CreateRequest((BoundaryRequest)null!),
            ClientFactoryEntryPoint.DestinationMessageAddress => factory.CreateRequest((Uri)null!, message),
            ClientFactoryEntryPoint.DestinationMessageMessage => factory.CreateRequest(destination, (BoundaryRequest)null!),
            ClientFactoryEntryPoint.CorrelatedMessageContext => factory.CreateRequest((ConsumeContext)null!, message),
            ClientFactoryEntryPoint.CorrelatedMessageMessage => factory.CreateRequest(consumeContext, (BoundaryRequest)null!),
            ClientFactoryEntryPoint.CorrelatedDestinationMessageContext =>
                factory.CreateRequest((ConsumeContext)null!, destination, message),
            ClientFactoryEntryPoint.CorrelatedDestinationMessageAddress =>
                factory.CreateRequest(consumeContext, (Uri)null!, message),
            ClientFactoryEntryPoint.CorrelatedDestinationMessageMessage =>
                factory.CreateRequest(consumeContext, destination, (BoundaryRequest)null!),
            ClientFactoryEntryPoint.Values => factory.CreateRequest<BoundaryRequest>((object)null!),
            ClientFactoryEntryPoint.DestinationValuesAddress => factory.CreateRequest<BoundaryRequest>((Uri)null!, values),
            ClientFactoryEntryPoint.DestinationValuesValues => factory.CreateRequest<BoundaryRequest>(destination, (object)null!),
            ClientFactoryEntryPoint.CorrelatedValuesContext =>
                factory.CreateRequest<BoundaryRequest>((ConsumeContext)null!, values),
            ClientFactoryEntryPoint.CorrelatedValuesValues =>
                factory.CreateRequest<BoundaryRequest>(consumeContext, (object)null!),
            ClientFactoryEntryPoint.CorrelatedDestinationValuesContext =>
                factory.CreateRequest<BoundaryRequest>((ConsumeContext)null!, destination, values),
            ClientFactoryEntryPoint.CorrelatedDestinationValuesAddress =>
                factory.CreateRequest<BoundaryRequest>(consumeContext, (Uri)null!, values),
            ClientFactoryEntryPoint.CorrelatedDestinationValuesValues =>
                factory.CreateRequest<BoundaryRequest>(consumeContext, destination, (object)null!),
            ClientFactoryEntryPoint.CorrelatedClientContext =>
                factory.CreateRequestClient<BoundaryRequest>((ConsumeContext)null!),
            ClientFactoryEntryPoint.DestinationClientAddress =>
                factory.CreateRequestClient<BoundaryRequest>((Uri)null!),
            ClientFactoryEntryPoint.CorrelatedDestinationClientContext =>
                factory.CreateRequestClient<BoundaryRequest>((ConsumeContext)null!, destination),
            ClientFactoryEntryPoint.CorrelatedDestinationClientAddress =>
                factory.CreateRequestClient<BoundaryRequest>(consumeContext, (Uri)null!),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint), entryPoint, null),
        };

        Assert.NotNull(result);
    }

    private static void Invoke(ScopedFactoryEntryPoint entryPoint, IScopedClientFactory factory)
    {
        var message = new BoundaryRequest("valid");
        var values = new { Value = "valid" };
        Uri destination = new("loopback://localhost/request");

        object result = entryPoint switch
        {
            ScopedFactoryEntryPoint.Message => factory.CreateRequest((BoundaryRequest)null!),
            ScopedFactoryEntryPoint.DestinationMessageAddress => factory.CreateRequest((Uri)null!, message),
            ScopedFactoryEntryPoint.DestinationMessageMessage => factory.CreateRequest(destination, (BoundaryRequest)null!),
            ScopedFactoryEntryPoint.Values => factory.CreateRequest<BoundaryRequest>((object)null!),
            ScopedFactoryEntryPoint.DestinationValuesAddress => factory.CreateRequest<BoundaryRequest>((Uri)null!, values),
            ScopedFactoryEntryPoint.DestinationValuesValues => factory.CreateRequest<BoundaryRequest>(destination, (object)null!),
            ScopedFactoryEntryPoint.DestinationClientAddress => factory.CreateRequestClient<BoundaryRequest>((Uri)null!),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint), entryPoint, null),
        };

        Assert.NotNull(result);
    }

    public enum EntryPoint
    {
        CreateTyped,
        CreateValues,
        SingleTyped,
        SingleValues,
        DoubleTyped,
        DoubleValues,
        TripleTyped,
        TripleValues,
    }

    public enum ClientFactoryEntryPoint
    {
        Message,
        DestinationMessageAddress,
        DestinationMessageMessage,
        CorrelatedMessageContext,
        CorrelatedMessageMessage,
        CorrelatedDestinationMessageContext,
        CorrelatedDestinationMessageAddress,
        CorrelatedDestinationMessageMessage,
        Values,
        DestinationValuesAddress,
        DestinationValuesValues,
        CorrelatedValuesContext,
        CorrelatedValuesValues,
        CorrelatedDestinationValuesContext,
        CorrelatedDestinationValuesAddress,
        CorrelatedDestinationValuesValues,
        CorrelatedClientContext,
        DestinationClientAddress,
        CorrelatedDestinationClientContext,
        CorrelatedDestinationClientAddress,
    }

    public enum ScopedFactoryEntryPoint
    {
        Message,
        DestinationMessageAddress,
        DestinationMessageMessage,
        Values,
        DestinationValuesAddress,
        DestinationValuesValues,
        DestinationClientAddress,
    }

    private sealed record BoundaryRequest(string Value);

    private sealed record BoundaryResponse(string Value);

    private sealed record AlternateResponse(string Value);

    private sealed record ThirdResponse(string Value);

    private sealed class RejectUnexpectedSendEndpoint : IRequestSendEndpoint<BoundaryRequest>
    {
        public int SendCount { get; private set; }

        public Task<BoundaryRequest> SendAsync(
            Guid requestId,
            object values,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<BoundaryRequest>(cancellationToken);

            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }

        public Task SendAsync(
            Guid requestId,
            BoundaryRequest message,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }
    }

    private sealed class ResolutionProbeRequestSendEndpoint : RequestSendEndpoint<BoundaryRequest>
    {
        public ResolutionProbeRequestSendEndpoint()
            : base(consumeContext: null)
        {
        }

        public int ResolutionCount { get; private set; }

        public CancellationToken ResolutionCancellationToken { get; private set; }

        protected override Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken)
        {
            ResolutionCount++;
            ResolutionCancellationToken = cancellationToken;
            return Task.FromException<ISendEndpoint>(new ResolutionProbeException());
        }
    }

    private class BoundaryClientFactoryContext : ClientFactoryContext
    {
        private int _resolutionCount;

        public int ResolutionCount => Volatile.Read(ref _resolutionCount);

        public RequestTimeout DefaultTimeout => RequestTimeout.Default;

        public TimeProvider TimeProvider => TimeProvider.System;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class
        {
            Interlocked.Increment(ref _resolutionCount);
            throw new NotSupportedException();
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class
        {
            Interlocked.Increment(ref _resolutionCount);
            throw new NotSupportedException();
        }
    }

    private sealed class DisposableBoundaryClientFactoryContext : BoundaryClientFactoryContext, IAsyncDisposable
    {
        private readonly TaskCompletionSource _disposalStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseDisposal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;

        public Task DisposalStarted => _disposalStarted.Task;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            _disposalStarted.TrySetResult();
            return new ValueTask(_releaseDisposal.Task);
        }

        public void ReleaseDisposal() => _releaseDisposal.TrySetResult();
    }

    private sealed class FaultingDisposableBoundaryClientFactoryContext(Exception failure) : BoundaryClientFactoryContext, IAsyncDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return new ValueTask(Task.FromException(failure));
        }
    }

    private class UnusedConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The consume context must not be read ({targetMethod?.Name}).");
    }

    private class RecordingBusProxy : DispatchProxy
    {
        public int BasicConnectionCount { get; private set; }

        public object? BasicPipe { get; private set; }

        public int ConfiguredConnectionCount { get; private set; }

        public object? ConfiguredPipe { get; private set; }

        public ConnectPipeOptions Options { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IConsumePipeConnector.ConnectConsumePipe) || args is null)
                throw new InvalidOperationException($"Unexpected bus invocation: {targetMethod?.Name}.");

            if (args.Length == 1)
            {
                BasicConnectionCount++;
                BasicPipe = args[0];
            }
            else
            {
                ConfiguredConnectionCount++;
                ConfiguredPipe = args[0];
                Options = Assert.IsType<ConnectPipeOptions>(args[1]);
            }

            return new EmptyConnectHandle();
        }
    }

    private class ResponseContextProxy : DispatchProxy
    {
        public HostInfo Host { get; set; } = null!;

        public BoundaryResponse Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Host" => Host,
            "get_Message" => Message,
            _ => throw new InvalidOperationException($"Unexpected response-context invocation: {targetMethod?.Name}."),
        };
    }

    private sealed class RecordingConnectHandle : ConnectHandle
    {
        public int DisconnectCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void Disconnect() => DisconnectCount++;

        public void Dispose() => DisposeCount++;
    }

    private sealed class ResolutionProbeException : Exception;

    private sealed class FactoryDisposalException : Exception;
}
