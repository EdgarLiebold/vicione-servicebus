using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using RetryPolicyContext = ViciOne.ServiceBus.Advanced.Middleware.RetryContext;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class SendContextExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "consume-context-transfer-is-coherent")]
    public void TransferConsumeContextHeaders_ReplacesStalePayloadAndTransfersCausalityAndApplicationHeaders()
    {
        Guid conversationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid correlationId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var inputAddress = new Uri("loopback://localhost/input");
        ConsumeContext staleContext = CreateConsumeContext(
            new TestHeaders(),
            new Uri("loopback://localhost/stale"));
        var sourceHeaders = new TestHeaders(
            ("application-new", "source"),
            ("application-existing", "source-must-not-overwrite"),
            (MessageHeaders.RedeliveryCount, 12));
        ConsumeContext sourceContext = CreateConsumeContext(
            sourceHeaders,
            inputAddress,
            conversationId,
            correlationId);
        SendContext destination = CreateSendContext(out ContextProxy destinationState);
        var destinationHeaders = new RecordingSendHeaders(("application-existing", "destination"));
        destinationState.Set(nameof(SendContext.Headers), destinationHeaders);
        destination.GetOrAddPayload(() => staleContext);

        destination.TransferConsumeContextHeaders(sourceContext);

        Assert.True(destination.TryGetPayload(out ConsumeContext? transferredPayload));
        Assert.Same(sourceContext, transferredPayload);
        Assert.Equal(inputAddress, destinationState.Get<Uri>(nameof(SendContext.SourceAddress)));
        Assert.Equal(conversationId, destinationState.Get<Guid?>(nameof(SendContext.ConversationId)));
        Assert.Equal(correlationId, destinationState.Get<Guid?>(nameof(SendContext.InitiatorId)));
        Assert.Equal("source", destinationHeaders.Values["application-new"]);
        Assert.Equal("destination", destinationHeaders.Values["application-existing"]);
        Assert.DoesNotContain(MessageHeaders.RedeliveryCount, destinationHeaders.Values.Keys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "request-fallback-causality")]
    public void TransferConsumeContextHeaders_UsesRequestIdOnlyWhenCorrelationIdIsAbsent()
    {
        Guid requestId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        ConsumeContext sourceContext = CreateConsumeContext(
            new TestHeaders(),
            new Uri("loopback://localhost/input"),
            requestId: requestId);
        SendContext destination = CreateSendContext(out ContextProxy destinationState);

        destination.TransferConsumeContextHeaders(sourceContext);

        Assert.Equal(requestId, destinationState.Get<Guid?>(nameof(SendContext.InitiatorId)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "redelivery-lineage")]
    public void RedeliveryOptions_IncrementCountAndReplaceIdentityOnlyWhenRequested()
    {
        Guid currentMessageId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        Guid originalMessageId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        ConsumeContext sourceContext = CreateConsumeContext(
            new TestHeaders(
                (MessageHeaders.RedeliveryCount, 3),
                (MessageHeaders.OriginalMessageId, originalMessageId.ToString("D"))),
            new Uri("loopback://localhost/input"));
        SendContext preserved = CreateSendContext(out ContextProxy preservedState);
        preservedState.Set(nameof(SendContext.MessageId), currentMessageId);
        SendContext replaced = CreateSendContext(out ContextProxy replacedState);
        replacedState.Set(nameof(SendContext.MessageId), currentMessageId);

        preserved.ApplyRedeliveryOptions(sourceContext, RedeliveryOptions.None);
        replaced.ApplyRedeliveryOptions(sourceContext, RedeliveryOptions.ReplaceMessageId);

        Assert.Equal(currentMessageId, preservedState.Get<Guid?>(nameof(SendContext.MessageId)));
        Assert.Equal(4, preserved.Headers.Get<int>(MessageHeaders.RedeliveryCount, default(int?)));
        Assert.NotEqual(currentMessageId, replacedState.Get<Guid?>(nameof(SendContext.MessageId)));
        Assert.Equal(originalMessageId.ToString(), replaced.Headers.Get<string>(MessageHeaders.OriginalMessageId));
        Assert.Equal(4, replaced.Headers.Get<int>(MessageHeaders.RedeliveryCount, default(int?)));
        Assert.Equal(originalMessageId, sourceContext.GetOriginalMessageId());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "message-id-fallback-lineage")]
    public void MessageIdentityLineage_FallsBackToCurrentIdsWhenNoOriginalHeaderExists()
    {
        Guid consumedMessageId = Guid.Parse("51000000-0000-0000-0000-000000000005");
        Guid outgoingMessageId = Guid.Parse("52000000-0000-0000-0000-000000000005");
        ConsumeContext sourceContext = CreateConsumeContext(
            new TestHeaders(),
            new Uri("loopback://localhost/input"),
            messageId: consumedMessageId);
        SendContext outgoingContext = CreateSendContext(out ContextProxy outgoingState);
        outgoingState.Set(nameof(SendContext.MessageId), outgoingMessageId);

        SendContext returned = outgoingContext.ReplaceMessageId(sourceContext);

        Assert.Same(outgoingContext, returned);
        Assert.Equal(consumedMessageId, sourceContext.GetOriginalMessageId());
        Assert.Equal(outgoingMessageId.ToString(), outgoingContext.Headers.Get<string>(MessageHeaders.OriginalMessageId));
        Assert.NotEqual(outgoingMessageId, outgoingState.Get<Guid?>(nameof(SendContext.MessageId)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "conversation-lineage")]
    public void StartNewConversation_PreservesPreviousConversationAndRequiresANonEmptyIdentifier()
    {
        Guid previous = Guid.Parse("60000000-0000-0000-0000-000000000006");
        Guid next = Guid.Parse("70000000-0000-0000-0000-000000000007");
        SendContext context = CreateSendContext(out ContextProxy state);
        state.Set(nameof(SendContext.ConversationId), previous);

        SendContext returned = context.StartNewConversation(next);

        Assert.Same(context, returned);
        Assert.Equal(next, state.Get<Guid?>(nameof(SendContext.ConversationId)));
        Assert.Equal(previous.ToString(), context.Headers.Get<string>(MessageHeaders.InitiatingConversationId));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => context.StartNewConversation(Guid.Empty));
        Assert.Equal("conversationId", exception.ParamName);
        Assert.Equal(next, state.Get<Guid?>(nameof(SendContext.ConversationId)));

        context.StartNewConversation();
        Assert.NotEqual(next, state.Get<Guid?>(nameof(SendContext.ConversationId)));
        Assert.Equal(next.ToString(), context.Headers.Get<string>(MessageHeaders.InitiatingConversationId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "required-boundaries")]
    public void PublicOperations_RejectEveryMissingRequiredInputBeforeReadingDependencies()
    {
        SendContext sendContext = CreateSendContext(out _);
        ConsumeContext consumeContext = CreateConsumeContext(new TestHeaders(), new Uri("loopback://localhost/input"));
        var dictionary = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();

        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.SetHostHeaders((SendHeaders)null!)).ParamName);
        Assert.Equal("adapter", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.SetHostHeaders<string>(null!, dictionary)).ParamName);
        Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => adapter.SetHostHeaders(null!)).ParamName);
        Assert.Equal("sendContext", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.TransferConsumeContextHeaders(null!, consumeContext)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() => sendContext.TransferConsumeContextHeaders(null!)).ParamName);
        Assert.Equal("sendContext", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.ApplyRedeliveryOptions(null!, consumeContext, RedeliveryOptions.None)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() => sendContext.ApplyRedeliveryOptions(null!, RedeliveryOptions.None)).ParamName);
        Assert.Equal("sendContext", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.ReplaceMessageId(null!, consumeContext)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() => sendContext.ReplaceMessageId(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.GetOriginalMessageId(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.StartNewConversation(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.StartNewConversation(null!, Guid.NewGuid())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "host-header-parity")]
    public void HostHeaders_ExposeTheSameCompleteMetadataThroughBothDestinationKinds()
    {
        var headers = new RecordingSendHeaders();
        var dictionary = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();

        headers.SetHostHeaders();
        adapter.SetHostHeaders(dictionary);

        string[] expectedKeys =
        [
            MessageHeaders.Host.MachineName,
            MessageHeaders.Host.ProcessName,
            MessageHeaders.Host.ProcessId,
            MessageHeaders.Host.Assembly,
            MessageHeaders.Host.AssemblyVersion,
            MessageHeaders.Host.ViciOneServiceBusVersion,
            MessageHeaders.Host.FrameworkVersion,
            MessageHeaders.Host.OperatingSystemVersion,
        ];
        Assert.Equal(expectedKeys.Order(), headers.Values.Keys.Order());
        Assert.Equal(expectedKeys.Order(), dictionary.Keys.Order());
        Assert.All(expectedKeys, key => Assert.Equal(headers.Values[key].ToString(), dictionary[key]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "fault-header-parity-and-detail")]
    public void ExceptionHeaders_ExposeBaseFailureConsumerAndRetryDetailsThroughBothDestinationKinds()
    {
        var timestamp = new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var inputAddress = new Uri("loopback://localhost/input");
        InvalidOperationException baseException = CreateFailureWithStackTrace();
        ExceptionReceiveContext context = DispatchProxy.Create<ExceptionReceiveContext, ContextProxy>();
        var state = (ContextProxy)(object)context;
        state.Set(nameof(ExceptionReceiveContext.Exception), new ApplicationException("wrapper", baseException));
        state.Set(nameof(ExceptionReceiveContext.ExceptionTimestamp), timestamp);
        state.Set(nameof(ReceiveContext.InputAddress), inputAddress);
        context.GetOrAddPayload<ConsumerFaultContext>(() => new TestConsumerFaultContext("urn:message:test", "TestConsumer"));
        context.GetOrAddPayload<RetryPolicyContext>(() => new TestRetryContext(4));
        var headers = new RecordingSendHeaders();
        var dictionary = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();

        headers.SetExceptionHeaders(context);
        adapter.SetExceptionHeaders(dictionary, context);

        Assert.Equal("fault", headers.Values[MessageHeaders.Reason]);
        Assert.Equal("base failure", headers.Values[MessageHeaders.FaultMessage]);
        Assert.Equal(inputAddress.ToString(), headers.Values[MessageHeaders.FaultInputAddress]);
        Assert.Equal(timestamp.ToString("O"), headers.Values[MessageHeaders.FaultTimestamp]);
        Assert.Equal("urn:message:test", headers.Values[MessageHeaders.FaultMessageType]);
        Assert.Equal("TestConsumer", headers.Values[MessageHeaders.FaultConsumerType]);
        Assert.Equal(4, headers.Values[MessageHeaders.FaultRetryCount]);
        Assert.Contains(nameof(InvalidOperationException), headers.Values[MessageHeaders.FaultExceptionType].ToString());
        Assert.Contains(MessageHeaders.FaultStackTrace, headers.Values.Keys);
        Assert.Equal(headers.Values.Keys.Order(), dictionary.Keys.Order());
        Assert.Equal("base failure", dictionary[MessageHeaders.FaultMessage]);
        Assert.Equal(inputAddress.ToString(), dictionary[MessageHeaders.FaultInputAddress]);
        Assert.Equal("4", dictionary[MessageHeaders.FaultRetryCount]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-METADATA", "fault-header-required-boundaries")]
    public void ExceptionHeaderOperations_RejectEveryMissingRequiredInputInDeclarationOrder()
    {
        var headers = new RecordingSendHeaders();
        var dictionary = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();
        ExceptionReceiveContext context = DispatchProxy.Create<ExceptionReceiveContext, ContextProxy>();

        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.SetExceptionHeaders(null!, context)).ParamName);
        Assert.Equal("exceptionContext", Assert.Throws<ArgumentNullException>(() => headers.SetExceptionHeaders(null!)).ParamName);
        Assert.Equal("adapter", Assert.Throws<ArgumentNullException>(() => SendContextExtensions.SetExceptionHeaders<string>(null!, dictionary, context)).ParamName);
        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() => adapter.SetExceptionHeaders(null!, context)).ParamName);
        Assert.Equal("exceptionContext", Assert.Throws<ArgumentNullException>(() => adapter.SetExceptionHeaders(dictionary, null!)).ParamName);
    }

    private static SendContext CreateSendContext(out ContextProxy state)
    {
        SendContext context = DispatchProxy.Create<SendContext, ContextProxy>();
        state = (ContextProxy)(object)context;
        state.Set(nameof(SendContext.Headers), new RecordingSendHeaders());
        return context;
    }

    private static ConsumeContext CreateConsumeContext(
        Headers headers,
        Uri inputAddress,
        Guid? conversationId = null,
        Guid? correlationId = null,
        Guid? requestId = null,
        Guid? messageId = null,
        Headers? transportHeaders = null)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ContextProxy>();
        var receiveState = (ContextProxy)(object)receiveContext;
        receiveState.Set(nameof(ReceiveContext.InputAddress), inputAddress);
        receiveState.Set(nameof(ReceiveContext.TransportHeaders), transportHeaders ?? new TestHeaders());
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ContextProxy>();
        var state = (ContextProxy)(object)context;
        state.Set(nameof(ConsumeContext.Headers), headers);
        state.Set(nameof(ConsumeContext.ReceiveContext), receiveContext);
        state.Set(nameof(ConsumeContext.ConversationId), conversationId);
        state.Set(nameof(ConsumeContext.CorrelationId), correlationId);
        state.Set(nameof(ConsumeContext.RequestId), requestId);
        state.Set(nameof(ConsumeContext.MessageId), messageId);
        state.Set(nameof(ConsumeContext.SerializerContext), CreateSerializerContext());
        return context;
    }

    private static SerializerContext CreateSerializerContext()
    {
        SerializerContext context = DispatchProxy.Create<SerializerContext, ContextProxy>();
        return context;
    }

    private static InvalidOperationException CreateFailureWithStackTrace()
    {
        try
        {
            throw new InvalidOperationException("base failure");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    private class ContextProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _properties = [];
        private readonly List<object> _payloads = [];

        public void Set(string name, object? value) => _properties[name] = value;

        public TValue Get<TValue>(string name) => (TValue)_properties[name]!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal))
                return _properties[targetMethod.Name[4..]];
            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                _properties[targetMethod.Name[4..]] = args![0];
                return null;
            }

            Type? payloadType = targetMethod.IsGenericMethod ? targetMethod.GetGenericArguments()[0] : null;
            switch (targetMethod.Name)
            {
                case nameof(PipeContext.HasPayloadType):
                    return _payloads.Any(payload => ((Type)args![0]!).IsInstanceOfType(payload));
                case nameof(PipeContext.TryGetPayload):
                    object? payload = _payloads.LastOrDefault(payloadType!.IsInstanceOfType);
                    args![0] = payload;
                    return payload is not null;
                case nameof(PipeContext.GetOrAddPayload):
                    object? existing = _payloads.LastOrDefault(payloadType!.IsInstanceOfType);
                    if (existing is not null)
                        return existing;
                    object added = ((Delegate)args![0]!).DynamicInvoke()
                        ?? throw new InvalidOperationException("The payload factory returned null.");
                    _payloads.Add(added);
                    return added;
                case nameof(PipeContext.AddOrUpdatePayload):
                    int index = _payloads.FindLastIndex(payloadType!.IsInstanceOfType);
                    object updated = index >= 0
                        ? ((Delegate)args![1]!).DynamicInvoke(_payloads[index])!
                        : ((Delegate)args![0]!).DynamicInvoke()!;
                    if (updated is null)
                        throw new InvalidOperationException("The payload factory returned null.");
                    if (index >= 0)
                        _payloads[index] = updated;
                    else
                        _payloads.Add(updated);
                    return updated;
                case "DeserializeObject":
                    return Deserialize(args![0], payloadType!, args.Length > 1 ? args[1] : null);
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }

        private static object? Deserialize(object? value, Type valueType, object? defaultValue)
        {
            if (value is null)
                return defaultValue;
            if (valueType.IsInstanceOfType(value))
                return value;
            if (valueType == typeof(Guid) && value is string text && Guid.TryParse(text, out Guid id))
                return id;
            return defaultValue;
        }
    }

    private sealed class TestHeaders(params (string Key, object Value)[] values) : Headers
    {
        private readonly Dictionary<string, object> _values = values.ToDictionary(pair => pair.Key, pair => pair.Value);

        public IEnumerable<KeyValuePair<string, object>> GetAll() => _values;

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value) => _values.TryGetValue(key, out value);

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : class => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : struct => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public IEnumerator<HeaderValue> GetEnumerator() =>
            _values.Select(pair => new HeaderValue(pair)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class RecordingSendHeaders(params (string Key, object Value)[] values) : SendHeaders
    {
        public Dictionary<string, object> Values { get; } = values.ToDictionary(pair => pair.Key, pair => pair.Value);

        public IEnumerable<KeyValuePair<string, object>> GetAll() => Values;

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value) => Values.TryGetValue(key, out value);

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : class => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : struct => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public void Set(string key, string? value) => Set(key, value, true);

        public void Set(string key, object? value, bool overwrite = true)
        {
            if (!overwrite && Values.ContainsKey(key))
                return;
            if (value is null)
                Values.Remove(key);
            else
                Values[key] = value;
        }

        public IEnumerator<HeaderValue> GetEnumerator() =>
            Values.Select(pair => new HeaderValue(pair)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StringHeaderAdapter : ITransportSetHeaderAdapter<string>
    {
        public void Set(IDictionary<string, string> dictionary, in HeaderValue headerValue) =>
            dictionary[headerValue.Key] = headerValue.Value.ToString()!;

        public void Set<TValue>(IDictionary<string, string> dictionary, in HeaderValue<TValue> headerValue) =>
            dictionary[headerValue.Key] = headerValue.Value is null ? string.Empty : headerValue.Value.ToString()!;
    }

    private sealed record TestConsumerFaultContext(string MessageType, string ConsumerType) : ConsumerFaultContext;

    private sealed class TestRetryContext(int retryCount) : RetryPolicyContext
    {
        public CancellationToken CancellationToken => default;
        public Exception Exception { get; } = new InvalidOperationException("retry failure");
        public int RetryAttempt => retryCount + 1;
        public int RetryCount => retryCount;
        public TimeSpan? Delay => null;
        public Type ContextType => typeof(ConsumeContext);

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PreRetryAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
