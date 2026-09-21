using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text.Json;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipRequestResponseDeepContractTests
{
    private static readonly Uri ActivityAddress = new("loopback://localhost/deep-request-activity");
    private static readonly Uri FaultAddress = new("loopback://localhost/deep-request-fault");
    private static readonly Uri InputAddress = new("loopback://localhost/deep-request-input");
    private static readonly Uri ResponseAddress = new("loopback://localhost/deep-request-response");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-request-projection-preserves-identity-fault-retry-and-default-subscription")]
    public async Task RequestProxy_ProjectsOptionalMetadataAndPreservesTheOriginalContextAndRequestIdentityAsync()
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("preserved");
        var endpoint = new RoutingSlipRecordingEndpoint();
        ConsumeContext<RequestMessage> context = CreateRequestContext(
            request,
            endpoint,
            requestId,
            ResponseAddress,
            FaultAddress,
            retryAttempt: 3,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingRequestProxy();

        await proxy.ConsumeAsync(context);

        Assert.Same(context, proxy.Context);
        IRoutingSlip routingSlip = Assert.IsAssignableFrom<IRoutingSlip>(Assert.Single(endpoint.Messages));
        Assert.Same(request, routingSlip.Variables[RoutingSlipRequestVariableNames.Request]);
        Assert.Equal(requestId, routingSlip.Variables[RoutingSlipRequestVariableNames.RequestId]);
        Assert.Equal(ResponseAddress, routingSlip.Variables[RoutingSlipRequestVariableNames.ResponseAddress]);
        Assert.Equal(FaultAddress, routingSlip.Variables[RoutingSlipRequestVariableNames.FaultAddress]);
        Assert.Equal(InputAddress, routingSlip.Variables[RoutingSlipRequestVariableNames.RequestAddress]);
        Assert.Equal(3, routingSlip.Variables[RoutingSlipRequestVariableNames.RetryAttempt]);
        Assert.Equal(InputAddress, Assert.Single(routingSlip.Subscriptions).Address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-request-metadata-materializes-camel-case-serialized-values-and-zero-retry")]
    public void RequestInfo_MaterializesCamelCaseSerializedValuesIncludingTheZeroRetryBoundary()
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("serialized");
        var values = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["request"] = JsonSerializer.SerializeToElement(request),
            ["requestId"] = JsonSerializer.SerializeToElement(requestId),
            ["responseAddress"] = JsonSerializer.SerializeToElement(ResponseAddress),
            ["faultAddress"] = JsonSerializer.SerializeToElement(FaultAddress),
            ["requestAddress"] = JsonSerializer.SerializeToElement(InputAddress),
            ["retryAttempt"] = JsonSerializer.SerializeToElement(0),
        };

        var info = new RoutingSlipRequestInfo<RequestMessage>(ServiceBusMetadataJson.ObjectDeserializer, values);

        Assert.Equal(request, info.Request);
        Assert.Equal(requestId, info.RequestId);
        Assert.Equal(ResponseAddress, info.ResponseAddress);
        Assert.Equal(FaultAddress, info.FaultAddress);
        Assert.Equal(InputAddress, info.RequestAddress);
        Assert.Equal(0, info.RetryAttempt);
    }

    [Theory]
    [InlineData(RoutingSlipRequestVariableNames.FaultAddress)]
    [InlineData(RoutingSlipRequestVariableNames.RequestAddress)]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-request-metadata-rejects-relative-optional-endpoints")]
    public void RequestInfo_RejectsRelativeOptionalEndpointAddresses(string variableName)
    {
        Dictionary<string, object> values = ValidVariables(new RequestMessage("invalid-address"), NewId.NextGuid());
        values[variableName] = new Uri("relative-address", UriKind.Relative);

        SerializationException exception = Assert.Throws<SerializationException>(() =>
            new RoutingSlipRequestInfo<RequestMessage>(ServiceBusMetadataJson.ObjectDeserializer, values));

        Assert.Contains(variableName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("absolute", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-request-variable-names-remain-private-owned-unique-and-stable")]
    public void RequestVariableNames_AreInternalUniqueAndStable()
    {
        Type owner = typeof(RoutingSlipRequestVariableNames);
        Dictionary<string, string> constants = owner
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal);

        Assert.True(owner.IsNotPublic);
        Assert.True(owner.IsAbstract && owner.IsSealed);
        Assert.Equal(6, constants.Count);
        Assert.Equal(constants.Count, constants.Values.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("RequestId", constants[nameof(RoutingSlipRequestVariableNames.RequestId)]);
        Assert.Equal("Request", constants[nameof(RoutingSlipRequestVariableNames.Request)]);
        Assert.Equal("FaultAddress", constants[nameof(RoutingSlipRequestVariableNames.FaultAddress)]);
        Assert.Equal("ResponseAddress", constants[nameof(RoutingSlipRequestVariableNames.ResponseAddress)]);
        Assert.Equal("RequestAddress", constants[nameof(RoutingSlipRequestVariableNames.RequestAddress)]);
        Assert.Equal("RetryAttempt", constants[nameof(RoutingSlipRequestVariableNames.RetryAttempt)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-completed-response-projects-original-identities-and-exact-route")]
    public async Task CompletedResponse_ProjectsOriginalIdentitiesAndUsesTheExactResponseRouteAsync()
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("completed");
        var response = new ResponseMessage("response");
        var transport = new RecordingTransportEndpoint();
        var provider = new RecordingSendEndpointProvider(transport.Endpoint);
        var completed = new CompletedEvent(ValidVariables(request, requestId));
        ConsumeContext<IRoutingSlipCompleted> context = CreateResponseContext(
            completed,
            provider,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingResponseProxy(FactoryOutcome.Success, response, new FaultMessage("unused"));

        await proxy.ConsumeAsync(context);

        Assert.Same(context, proxy.CompletedContext);
        Assert.Same(request, proxy.CompletedRequest);
        Resolution resolution = Assert.Single(provider.Resolutions);
        Assert.Equal(ResponseAddress, resolution.Address);
        Assert.Equal(TestContext.Current.CancellationToken, resolution.CancellationToken);
        SentMessage sent = Assert.Single(transport.Messages);
        Assert.Same(response, sent.Message);
        Assert.Equal(requestId, sent.RequestId);
        Assert.Equal(TestContext.Current.CancellationToken, sent.CancellationToken);
    }

    [Theory]
    [InlineData(false, false, 0, false)]
    [InlineData(true, true, 0, false)]
    [InlineData(true, true, 2, true)]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-fault-response-selects-route-and-emits-only-positive-retry-count")]
    public async Task FaultedResponse_SelectsTheFaultRouteAndEmitsOnlyAPositiveRetryCountAsync(
        bool includeFaultAddress,
        bool includeRetryAttempt,
        int retryAttempt,
        bool expectRetryHeader)
    {
        Guid requestId = NewId.NextGuid();
        var request = new RequestMessage("faulted");
        var fault = new FaultMessage("fault-response");
        Dictionary<string, object> variables = ValidVariables(request, requestId);
        if (includeFaultAddress)
            variables[RoutingSlipRequestVariableNames.FaultAddress] = FaultAddress;
        if (includeRetryAttempt)
            variables[RoutingSlipRequestVariableNames.RetryAttempt] = retryAttempt;
        var transport = new RecordingTransportEndpoint();
        var provider = new RecordingSendEndpointProvider(transport.Endpoint);
        var faulted = new FaultedEvent(variables);
        ConsumeContext<IRoutingSlipFaulted> context = CreateResponseContext(
            faulted,
            provider,
            TestContext.Current.CancellationToken);
        var proxy = new CapturingResponseProxy(FactoryOutcome.Success, new ResponseMessage("unused"), fault);

        await proxy.ConsumeAsync(context);

        Assert.Same(context, proxy.FaultedContext);
        Assert.Same(request, proxy.FaultedRequest);
        Assert.Equal(requestId, proxy.FaultedRequestId);
        Assert.Equal(includeFaultAddress ? FaultAddress : ResponseAddress, Assert.Single(provider.Resolutions).Address);
        SentMessage sent = Assert.Single(transport.Messages);
        Assert.Same(fault, sent.Message);
        Assert.Equal(requestId, sent.RequestId);
        Assert.Equal(expectRetryHeader, sent.Headers.TryGetValue(MessageHeaders.FaultRetryCount, out object? header));
        if (expectRetryHeader)
            Assert.Equal(retryAttempt, header);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-response-proxy-rejects-null-contexts-with-declared-parameter-name")]
    public async Task ResponseProxy_RejectsNullContextsWithTheDeclaredParameterNameAsync(bool faulted)
    {
        var proxy = new CapturingResponseProxy(
            FactoryOutcome.Success,
            new ResponseMessage("unused"),
            new FaultMessage("unused"));

        Func<Task> consume = faulted
            ? () => proxy.ConsumeAsync((ConsumeContext<IRoutingSlipFaulted>)null!)
            : () => proxy.ConsumeAsync((ConsumeContext<IRoutingSlipCompleted>)null!);
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(consume);

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(0, proxy.FactoryCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-pre-canceled-response-proxy-skips-resolution-factory-and-send")]
    public async Task ResponseProxy_WithPreCanceledDeliverySkipsEndpointFactoryAndSendAsync(bool faulted)
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var transport = new RecordingTransportEndpoint();
        var provider = new RecordingSendEndpointProvider(transport.Endpoint);
        Dictionary<string, object> variables = ValidVariables(new RequestMessage("canceled"), NewId.NextGuid());
        var proxy = new CapturingResponseProxy(
            FactoryOutcome.Success,
            new ResponseMessage("unused"),
            new FaultMessage("unused"));

        Func<Task> consume = faulted
            ? () => proxy.ConsumeAsync(CreateResponseContext(new FaultedEvent(variables), provider, cancellation.Token))
            : () => proxy.ConsumeAsync(CreateResponseContext(new CompletedEvent(variables), provider, cancellation.Token));
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(consume);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(provider.Resolutions);
        Assert.Equal(0, proxy.FactoryCallCount);
        Assert.Empty(transport.Messages);
    }

    public static TheoryData<bool, FactoryOutcome> InvalidFactoryOutcomes => new()
    {
        { false, FactoryOutcome.NullTask },
        { true, FactoryOutcome.NullTask },
        { false, FactoryOutcome.NullResult },
        { true, FactoryOutcome.NullResult },
        { false, FactoryOutcome.Failure },
        { true, FactoryOutcome.Failure },
    };

    [Theory]
    [MemberData(nameof(InvalidFactoryOutcomes))]
    [RequirementCoverage("REQ-VSB-COURIER-REQUESTS", "deep-response-factory-null-task-null-result-and-failure-contracts")]
    public async Task ResponseProxy_PreservesExplicitFactoryFailureContractsAsync(bool faulted, FactoryOutcome outcome)
    {
        var transport = new RecordingTransportEndpoint();
        var provider = new RecordingSendEndpointProvider(transport.Endpoint);
        Dictionary<string, object> variables = ValidVariables(new RequestMessage("factory-boundary"), NewId.NextGuid());
        var proxy = new CapturingResponseProxy(
            outcome,
            new ResponseMessage("unused"),
            new FaultMessage("unused"));
        Func<Task> consume = faulted
            ? () => proxy.ConsumeAsync(CreateResponseContext(
                new FaultedEvent(variables), provider, TestContext.Current.CancellationToken))
            : () => proxy.ConsumeAsync(CreateResponseContext(
                new CompletedEvent(variables), provider, TestContext.Current.CancellationToken));

        if (outcome == FactoryOutcome.Failure)
        {
            FactoryException exception = await Assert.ThrowsAsync<FactoryException>(consume);
            Assert.Same(proxy.FactoryFailure, exception);
        }
        else
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(consume);
            Assert.Contains("null", exception.Message, StringComparison.OrdinalIgnoreCase);
            if (outcome == FactoryOutcome.NullTask)
            {
                Assert.Contains("null task", exception.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(
                    faulted ? "CreateFaultedResponseMessageAsync" : "CreateResponseMessageAsync",
                    exception.Message,
                    StringComparison.Ordinal);
            }
            else
                Assert.Contains(faulted ? "fault response" : "response", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Single(provider.Resolutions);
        Assert.Equal(1, proxy.FactoryCallCount);
        Assert.Empty(transport.Messages);
    }

    private static Dictionary<string, object> ValidVariables(RequestMessage request, Guid requestId) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RoutingSlipRequestVariableNames.Request] = request,
            [RoutingSlipRequestVariableNames.RequestId] = requestId,
            [RoutingSlipRequestVariableNames.ResponseAddress] = ResponseAddress,
        };

    private static ConsumeContext<RequestMessage> CreateRequestContext(
        RequestMessage request,
        RoutingSlipRecordingEndpoint endpoint,
        Guid requestId,
        Uri responseAddress,
        Uri? faultAddress,
        int retryAttempt,
        CancellationToken cancellationToken)
    {
        TestConsumeContext<RequestMessage> context =
            DispatchProxy.Create<TestConsumeContext<RequestMessage>, RequestConsumeContextProxy<RequestMessage>>();
        var proxy = (RequestConsumeContextProxy<RequestMessage>)(object)context;
        proxy.Message = request;
        proxy.Endpoint = endpoint;
        proxy.RequestId = requestId;
        proxy.ResponseAddress = responseAddress;
        proxy.FaultAddress = faultAddress;
        proxy.CancellationToken = cancellationToken;
        proxy.Headers = CreateHeaders(retryAttempt);
        proxy.ReceiveContext = CreateReceiveContext(new RecordingSendEndpointProvider(endpoint));
        return context;
    }

    private static ConsumeContext<T> CreateResponseContext<T>(
        T message,
        RecordingSendEndpointProvider provider,
        CancellationToken cancellationToken)
        where T : class
    {
        TestConsumeContext<T> context = DispatchProxy.Create<TestConsumeContext<T>, ResponseConsumeContextProxy<T>>();
        var proxy = (ResponseConsumeContextProxy<T>)(object)context;
        proxy.Message = message;
        proxy.CancellationToken = cancellationToken;
        proxy.ReceiveContext = CreateReceiveContext(provider);
        proxy.SerializerContext = CreateSerializerContext();
        proxy.Headers = CreateHeaders(retryAttempt: null);
        return context;
    }

    private static ReceiveContext CreateReceiveContext(ISendEndpointProvider provider)
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        var proxy = (ReceiveContextProxy)(object)context;
        proxy.InputAddress = InputAddress;
        proxy.SendEndpointProvider = provider;
        return context;
    }

    private static SerializerContext CreateSerializerContext()
    {
        SerializerContext context = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        ((SerializerContextProxy)(object)context).Deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        return context;
    }

    private static Headers CreateHeaders(int? retryAttempt)
    {
        Headers headers = DispatchProxy.Create<Headers, HeadersProxy>();
        ((HeadersProxy)(object)headers).RetryAttempt = retryAttempt;
        return headers;
    }

    private sealed record RequestMessage(string Value);

    private sealed record ResponseMessage(string Value);

    private sealed record FaultMessage(string Value);

    public enum FactoryOutcome
    {
        Success,
        NullTask,
        NullResult,
        Failure,
    }

    private sealed class FactoryException : Exception;

    private sealed class CompletedEvent(IReadOnlyDictionary<string, object> variables) : IRoutingSlipCompleted
    {
        public Guid TrackingNumber { get; } = NewId.NextGuid();
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
        public TimeSpan Duration => TimeSpan.Zero;
        public IReadOnlyDictionary<string, object> Variables { get; } = variables;
    }

    private sealed class FaultedEvent(IReadOnlyDictionary<string, object> variables) : IRoutingSlipFaulted
    {
        public Guid TrackingNumber { get; } = NewId.NextGuid();
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
        public TimeSpan Duration => TimeSpan.Zero;
        public IReadOnlyList<IActivityException> ActivityExceptions => [];
        public IReadOnlyDictionary<string, object> Variables { get; } = variables;
    }

    private sealed class CapturingRequestProxy : RoutingSlipRequestProxy<RequestMessage>
    {
        public ConsumeContext<RequestMessage>? Context { get; private set; }

        protected override Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<RequestMessage> request)
        {
            Context = request;
            builder.AddActivity("deep-request", ActivityAddress, new { request.Message.Value });
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingResponseProxy(
        FactoryOutcome outcome,
        ResponseMessage response,
        FaultMessage fault) : RoutingSlipResponseProxy<RequestMessage, ResponseMessage, FaultMessage>
    {
        public ConsumeContext<IRoutingSlipCompleted>? CompletedContext { get; private set; }
        public RequestMessage? CompletedRequest { get; private set; }
        public FactoryException FactoryFailure { get; } = new();
        public int FactoryCallCount { get; private set; }
        public ConsumeContext<IRoutingSlipFaulted>? FaultedContext { get; private set; }
        public RequestMessage? FaultedRequest { get; private set; }
        public Guid FaultedRequestId { get; private set; }

        protected override Task<ResponseMessage> CreateResponseMessageAsync(
            ConsumeContext<IRoutingSlipCompleted> context,
            RequestMessage request)
        {
            FactoryCallCount++;
            CompletedContext = context;
            CompletedRequest = request;
            return CreateAsync(outcome, response);
        }

        protected override Task<FaultMessage> CreateFaultedResponseMessageAsync(
            ConsumeContext<IRoutingSlipFaulted> context,
            RequestMessage request,
            Guid requestId)
        {
            FactoryCallCount++;
            FaultedContext = context;
            FaultedRequest = request;
            FaultedRequestId = requestId;
            return CreateAsync(outcome, fault);
        }

        private Task<T> CreateAsync<T>(FactoryOutcome configuredOutcome, T value)
            where T : class => configuredOutcome switch
            {
                FactoryOutcome.Success => Task.FromResult(value),
                FactoryOutcome.NullTask => null!,
                FactoryOutcome.NullResult => Task.FromResult<T>(null!),
                FactoryOutcome.Failure => Task.FromException<T>(FactoryFailure),
                _ => throw new ArgumentOutOfRangeException(nameof(configuredOutcome)),
            };
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class RequestConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public CancellationToken CancellationToken { get; set; }
        public RoutingSlipRecordingEndpoint Endpoint { get; set; } = null!;
        public Uri? FaultAddress { get; set; }
        public Headers Headers { get; set; } = null!;
        public T Message { get; set; } = null!;
        public ReceiveContext ReceiveContext { get; set; } = null!;
        public Guid? RequestId { get; set; }
        public Uri? ResponseAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_FaultAddress" => FaultAddress,
            "get_Headers" => Headers,
            "get_Message" => Message,
            "get_ReceiveContext" => ReceiveContext,
            "get_RequestId" => RequestId,
            "get_ResponseAddress" => ResponseAddress,
            "TryGetPayload" => SetTimeProvider(args),
            "GetSendEndpointAsync" => GetEndpointAsync(args),
            _ => throw new NotSupportedException($"Unexpected request consume-context member: {targetMethod?.Name}"),
        };

        private Task<ISendEndpoint> GetEndpointAsync(object?[]? args)
        {
            CancellationToken token = args is { Length: > 1 } && args[1] is CancellationToken value ? value : default;
            token.ThrowIfCancellationRequested();
            return Task.FromResult<ISendEndpoint>(Endpoint);
        }

        private static bool SetTimeProvider(object?[]? args)
        {
            if (args is null || args.Length == 0)
                return false;

            args[0] = TimeProvider.System;
            return true;
        }
    }

    private class ResponseConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public CancellationToken CancellationToken { get; set; }
        public Headers Headers { get; set; } = null!;
        public T Message { get; set; } = null!;
        public ReceiveContext ReceiveContext { get; set; } = null!;
        public SerializerContext SerializerContext { get; set; } = null!;
        public List<Task> Tasks { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_ConversationId" => null,
            "get_CorrelationId" => null,
            "get_ExpirationTime" => null,
            "get_Headers" => Headers,
            "get_Message" => Message,
            "get_ReceiveContext" => ReceiveContext,
            "get_RequestId" => null,
            "get_SerializerContext" => SerializerContext,
            "AddConsumeTask" => AddTask(args),
            _ => throw new NotSupportedException($"Unexpected response consume-context member: {targetMethod?.Name}"),
        };

        private object? AddTask(object?[]? args)
        {
            Tasks.Add(Assert.IsAssignableFrom<Task>(Assert.Single(args!)));
            return null;
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public Uri InputAddress { get; set; } = null!;
        public ISendEndpointProvider SendEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => InputAddress,
            "get_SendEndpointProvider" => SendEndpointProvider,
            _ => throw new NotSupportedException($"Unexpected receive-context member: {targetMethod?.Name}"),
        };
    }

    private class SerializerContextProxy : DispatchProxy
    {
        public IObjectDeserializer Deserializer { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IObjectDeserializer.DeserializeObject))
                throw new NotSupportedException($"Unexpected serializer-context member: {targetMethod?.Name}");

            try
            {
                return targetMethod.Invoke(Deserializer, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private class HeadersProxy : DispatchProxy
    {
        public int? RetryAttempt { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "Get" => RetryAttempt,
            "GetAll" => Array.Empty<KeyValuePair<string, object>>(),
            "GetEnumerator" => ((IEnumerable<HeaderValue>)Array.Empty<HeaderValue>()).GetEnumerator(),
            "TryGetHeader" => TryGetHeader(args),
            _ => throw new NotSupportedException($"Unexpected headers member: {targetMethod?.Name}"),
        };

        private bool TryGetHeader(object?[]? args)
        {
            bool found = RetryAttempt.HasValue
                && args is { Length: > 1 }
                && string.Equals(args[0] as string, MessageHeaders.Request.RoutingSlipRetryCount, StringComparison.Ordinal);
            if (args is { Length: > 1 })
                args[1] = found ? RetryAttempt : null;
            return found;
        }
    }

    private sealed class RoutingSlipRecordingEndpoint : ISendEndpoint
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

    private sealed class RecordingSendEndpointProvider(ISendEndpoint endpoint) : ISendEndpointProvider
    {
        public List<Resolution> Resolutions { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Resolutions.Add(new Resolution(address, cancellationToken));
            return Task.FromResult(endpoint);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private sealed class RecordingTransportEndpoint
    {
        public RecordingTransportEndpoint()
        {
            ITransportSendEndpoint endpoint = DispatchProxy.Create<ITransportSendEndpoint, TransportEndpointProxy>();
            ((TransportEndpointProxy)(object)endpoint).Owner = this;
            Endpoint = endpoint;
        }

        public ISendEndpoint Endpoint { get; }
        public List<SentMessage> Messages { get; } = [];

        private async Task RecordAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sendContext = new MessageSendContext<T>(message, cancellationToken);
            await pipe.SendAsync(sendContext);
            Messages.Add(new SentMessage(
                message,
                sendContext.RequestId,
                sendContext.Headers.GetAll().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal),
                cancellationToken));
        }

        private class TransportEndpointProxy : DispatchProxy
        {
            public RecordingTransportEndpoint Owner { get; set; } = null!;

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod?.Name == nameof(IAdvancedSendEndpoint.SendAsync)
                    && targetMethod.IsGenericMethod
                    && args is { Length: 3 })
                {
                    MethodInfo record = typeof(RecordingTransportEndpoint)
                        .GetMethod(nameof(RecordAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;
                    return record.MakeGenericMethod(targetMethod.GetGenericArguments()).Invoke(Owner, args);
                }

                throw new NotSupportedException($"Unexpected transport endpoint member: {targetMethod?.Name}");
            }
        }
    }

    private sealed record Resolution(Uri Address, CancellationToken CancellationToken);

    private sealed record SentMessage(
        object Message,
        Guid? RequestId,
        IReadOnlyDictionary<string, object> Headers,
        CancellationToken CancellationToken);
}
