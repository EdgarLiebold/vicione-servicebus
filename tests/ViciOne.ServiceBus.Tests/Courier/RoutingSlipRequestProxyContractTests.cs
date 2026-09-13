using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipRequestProxyContractTests
{
    private static readonly DateTimeOffset ContextTime = new(2040, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly Uri ActivityAddress = new("loopback://localhost/activity");
    private static readonly Uri ClientResponseAddress = new("loopback://localhost/client-response");
    private static readonly Uri InputAddress = new("loopback://localhost/request");
    private static readonly Uri ResponseEndpointAddress = new("loopback://localhost/routing-slip-response");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "context-clock-requester-address-and-subscription-address-are-preserved")]
    public async Task ConsumeAsync_PreservesTheClockAndTheTwoDistinctResponseAddressesAsync()
    {
        Guid requestId = NewId.NextGuid();
        var endpoint = new RecordingEndpoint();
        ConsumeContext<RequestMessage> context = CreateContext(
            new RequestMessage("value"), endpoint, new FakeTimeProvider(ContextTime), requestId, ClientResponseAddress,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingRequestProxy(ResponseEndpointAddress);

        await proxy.ConsumeAsync(context);

        IRoutingSlip routingSlip = Assert.IsAssignableFrom<IRoutingSlip>(Assert.Single(endpoint.Messages));
        Assert.Equal(ContextTime, routingSlip.CreateTimestamp);
        Assert.Equal(requestId, routingSlip.Variables[RoutingSlipRequestVariableNames.RequestId]);
        Assert.Equal(ClientResponseAddress, routingSlip.Variables[RoutingSlipRequestVariableNames.ResponseAddress]);
        Assert.Equal(InputAddress, routingSlip.Variables[RoutingSlipRequestVariableNames.RequestAddress]);
        Assert.Equal(new RequestMessage("value"), routingSlip.Variables[RoutingSlipRequestVariableNames.Request]);
        Assert.Single(routingSlip.Subscriptions);
        Assert.Equal(ResponseEndpointAddress, routingSlip.Subscriptions[0].Address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "request-id-required-before-builder-callback")]
    public async Task ConsumeAsync_RejectsAMissingRequestIdentifierBeforeBuildingTheRoutingSlipAsync()
    {
        var endpoint = new RecordingEndpoint();
        ConsumeContext<RequestMessage> context = CreateContext(
            new RequestMessage("value"), endpoint, new FakeTimeProvider(ContextTime), requestId: null, ResponseEndpointAddress,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingRequestProxy(ResponseEndpointAddress);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ConsumeAsync(context));

        Assert.Contains("request identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, proxy.BuildCount);
        Assert.Empty(endpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "requester-response-address-required-before-builder-callback")]
    public async Task ConsumeAsync_RejectsAMissingRequesterResponseAddressBeforeBuildingTheRoutingSlipAsync()
    {
        var endpoint = new RecordingEndpoint();
        ConsumeContext<RequestMessage> context = CreateContext(
            new RequestMessage("value"), endpoint, new FakeTimeProvider(ContextTime), NewId.NextGuid(), responseAddress: null,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingRequestProxy(ResponseEndpointAddress);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ConsumeAsync(context));

        Assert.Contains("response address", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, proxy.BuildCount);
        Assert.Empty(endpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "null-builder-task-has-explicit-diagnostic")]
    public async Task ConsumeAsync_RejectsANullRoutingSlipBuilderTaskWithAnExplicitDiagnosticAsync()
    {
        var endpoint = new RecordingEndpoint();
        ConsumeContext<RequestMessage> context = CreateContext(
            new RequestMessage("value"), endpoint, new FakeTimeProvider(ContextTime), NewId.NextGuid(), ResponseEndpointAddress,
            TestContext.Current.CancellationToken);
        var proxy = new NullTaskRequestProxy();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ConsumeAsync(context));

        Assert.Contains(nameof(RoutingSlipRequestProxy<RequestMessage>.ConsumeAsync), exception.Message, StringComparison.Ordinal);
        Assert.Contains("null task", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(endpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "pre-canceled-request-proxy-skips-builder-and-transport")]
    public async Task ConsumeAsync_WithPreCanceledContextSkipsBuilderAndTransportAsync()
    {
        var endpoint = new RecordingEndpoint();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        ConsumeContext<RequestMessage> context = CreateContext(
            new RequestMessage("value"), endpoint, new FakeTimeProvider(ContextTime), NewId.NextGuid(), ResponseEndpointAddress, cancellation.Token);
        var proxy = new CapturingRequestProxy(ResponseEndpointAddress);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => proxy.ConsumeAsync(context));

        Assert.Equal(0, proxy.BuildCount);
        Assert.Empty(endpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "request-metadata-round-trips-required-and-optional-values")]
    public void RequestInfo_RestoresValidatedRequiredAndOptionalMetadata()
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("value");
        var faultAddress = new Uri("loopback://localhost/fault");
        var values = ValidRequestVariables(requestId, request);
        values[RoutingSlipRequestVariableNames.FaultAddress] = faultAddress;
        values[RoutingSlipRequestVariableNames.RequestAddress] = InputAddress;
        values[RoutingSlipRequestVariableNames.RetryAttempt] = 2;

        var info = new RoutingSlipRequestInfo<RequestMessage>(ServiceBusMetadataJson.ObjectDeserializer, values);

        Assert.Equal(request, info.Request);
        Assert.Equal(requestId, info.RequestId);
        Assert.Equal(ResponseEndpointAddress, info.ResponseAddress);
        Assert.Equal(faultAddress, info.FaultAddress);
        Assert.Equal(InputAddress, info.RequestAddress);
        Assert.Equal(2, info.RetryAttempt);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "request-metadata-rejects-missing-or-invalid-state")]
    public void RequestInfo_RejectsMissingOrInvalidRequiredMetadata()
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("value");
        var cases = new List<Dictionary<string, object>>
        {
            ValidRequestVariables(requestId, request),
            ValidRequestVariables(requestId, request),
            ValidRequestVariables(requestId, request),
            ValidRequestVariables(Guid.Empty, request),
            ValidRequestVariables(requestId, request),
            ValidRequestVariables(requestId, request),
        };
        cases[0].Remove(RoutingSlipRequestVariableNames.Request);
        cases[1].Remove(RoutingSlipRequestVariableNames.RequestId);
        cases[2].Remove(RoutingSlipRequestVariableNames.ResponseAddress);
        cases[4][RoutingSlipRequestVariableNames.ResponseAddress] = new Uri("relative-response", UriKind.Relative);
        cases[5][RoutingSlipRequestVariableNames.RetryAttempt] = -1;

        foreach (Dictionary<string, object> values in cases)
        {
            SerializationException exception = Assert.Throws<SerializationException>(() =>
                new RoutingSlipRequestInfo<RequestMessage>(ServiceBusMetadataJson.ObjectDeserializer, values));
            Assert.NotEmpty(exception.Message);
        }

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new RoutingSlipRequestInfo<RequestMessage>(null!, ValidRequestVariables(requestId, request))).ParamName);
        Assert.Equal("variables", Assert.Throws<ArgumentNullException>(() =>
            new RoutingSlipRequestInfo<RequestMessage>(ServiceBusMetadataJson.ObjectDeserializer, null!)).ParamName);
    }

    private static Dictionary<string, object> ValidRequestVariables(Guid requestId, RequestMessage request) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RoutingSlipRequestVariableNames.Request] = request,
            [RoutingSlipRequestVariableNames.RequestId] = requestId,
            [RoutingSlipRequestVariableNames.ResponseAddress] = ResponseEndpointAddress,
        };

    private static ConsumeContext<RequestMessage> CreateContext(
        RequestMessage message,
        RecordingEndpoint endpoint,
        TimeProvider timeProvider,
        Guid? requestId,
        Uri? responseAddress,
        CancellationToken cancellationToken = default)
    {
        TestConsumeContext<RequestMessage> context = DispatchProxy.Create<TestConsumeContext<RequestMessage>, ConsumeContextProxy<RequestMessage>>();
        var proxy = (ConsumeContextProxy<RequestMessage>)(object)context;
        proxy.Message = message;
        proxy.Endpoint = endpoint;
        proxy.TimeProvider = timeProvider;
        proxy.RequestId = requestId;
        proxy.ResponseAddress = responseAddress;
        proxy.CancellationToken = cancellationToken;
        proxy.Headers = DispatchProxy.Create<Headers, HeadersProxy>();

        TestReceiveContext receiveContext = DispatchProxy.Create<TestReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).InputAddress = InputAddress;
        proxy.ReceiveContext = receiveContext;
        return context;
    }

    private sealed record RequestMessage(string Value);

    private sealed class CapturingRequestProxy(Uri responseEndpointAddress) : RoutingSlipRequestProxy<RequestMessage>
    {
        public int BuildCount { get; private set; }

        protected override Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<RequestMessage> request)
        {
            BuildCount++;
            builder.AddActivity("Activity", ActivityAddress, new { request.Message.Value });
            return Task.CompletedTask;
        }

        protected override Uri GetResponseEndpointAddress(ConsumeContext<RequestMessage> context) => responseEndpointAddress;
    }

    private sealed class NullTaskRequestProxy : RoutingSlipRequestProxy<RequestMessage>
    {
        protected override Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<RequestMessage> request) => null!;

        protected override Uri GetResponseEndpointAddress(ConsumeContext<RequestMessage> context) => ResponseEndpointAddress;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private interface TestReceiveContext : ReceiveContext;

    private class ConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public CancellationToken CancellationToken { get; set; }
        public RecordingEndpoint Endpoint { get; set; } = null!;
        public Uri? FaultAddress { get; set; }
        public Headers Headers { get; set; } = null!;
        public T Message { get; set; } = null!;
        public ReceiveContext ReceiveContext { get; set; } = null!;
        public Guid? RequestId { get; set; }
        public Uri? ResponseAddress { get; set; }
        public TimeProvider TimeProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => CancellationToken,
                "get_FaultAddress" => FaultAddress,
                "get_Headers" => Headers,
                "get_Message" => Message,
                "get_ReceiveContext" => ReceiveContext,
                "get_RequestId" => RequestId,
                "get_ResponseAddress" => ResponseAddress,
                "TryGetPayload" => SetPayload(args),
                "GetSendEndpointAsync" => GetEndpointAsync(args),
                _ => throw new NotSupportedException($"Unexpected consume-context member: {targetMethod?.Name}"),
            };
        }

        private Task<ISendEndpoint> GetEndpointAsync(object?[]? args)
        {
            CancellationToken cancellationToken = args is { Length: > 1 } && args[1] is CancellationToken token ? token : default;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ISendEndpoint>(Endpoint);
        }

        private bool SetPayload(object?[]? args)
        {
            if (args is null || args.Length == 0)
                return false;

            args[0] = TimeProvider;
            return true;
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public Uri InputAddress { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => InputAddress,
            _ => throw new NotSupportedException($"Unexpected receive-context member: {targetMethod?.Name}"),
        };
    }

    private class HeadersProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "Get" => null,
            "GetAll" => Array.Empty<KeyValuePair<string, object>>(),
            "GetEnumerator" => ((IEnumerable<HeaderValue>)Array.Empty<HeaderValue>()).GetEnumerator(),
            "TryGetHeader" => SetMissing(args),
            _ => throw new NotSupportedException($"Unexpected headers member: {targetMethod?.Name}"),
        };

        private static bool SetMissing(object?[]? args)
        {
            if (args is { Length: > 1 })
                args[1] = null;
            return false;
        }
    }

    private sealed class RecordingEndpoint : ISendEndpoint
    {
        public List<object> Messages { get; } = [];

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }
}
