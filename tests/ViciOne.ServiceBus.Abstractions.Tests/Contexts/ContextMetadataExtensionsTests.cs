using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class ContextMetadataExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "message-type-normalization")]
    public void MessageTypes_AreTrimmedAndEmptyEntriesAreRemoved()
    {
        var headers = new TestHeaders((MessageHeaders.MessageType, " urn:message:first ; ;urn:message:second "));

        Assert.Equal(["urn:message:first", "urn:message:second"], headers.GetMessageTypes());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "identifier-and-address-validation")]
    public void IdentifiersAndAddresses_RejectInvalidTransportValues()
    {
        Guid expectedId = Guid.Parse("9d8a64aa-3b42-4cf4-a923-4e59ee9a615e");
        Guid fallback = Guid.Parse("2096a463-1f5a-4cf7-98cb-cb6507cebb34");
        var headers = new TestHeaders(
            ("valid-id", expectedId.ToString("D")),
            ("invalid-id", "not-an-id"),
            ("absolute", "loopback://service/queue"),
            ("relative", new Uri("queue", UriKind.Relative)));

        Assert.Equal(expectedId, headers.GetHeaderId("valid-id"));
        Assert.Equal(fallback, headers.GetHeaderId("invalid-id", fallback));
        Assert.Equal(new Uri("loopback://service/queue"), headers.GetEndpointAddress("absolute"));
        Assert.Null(headers.GetEndpointAddress("relative"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "invariant-utc-sent-time")]
    public void SentTime_IsParsedInvariantlyAndNormalizedToUtc()
    {
        var headers = new TestHeaders((MessageHeaders.TransportSentTime, "2031-02-03T04:05:06+02:00"));
        ReceiveContext context = CreateReceiveContext(headers);

        DateTimeOffset? actual = context.GetSentTime();

        Assert.Equal(new DateTimeOffset(2031, 2, 3, 2, 5, 6, TimeSpan.Zero), actual);
        Assert.Equal(TimeSpan.Zero, actual?.Offset);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "all-supported-timestamp-representations-normalize-to-utc")]
    public void SentTime_NormalizesEverySupportedTimestampRepresentationToUtc()
    {
        var offset = new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.FromHours(2));
        var utc = new DateTime(2031, 2, 3, 2, 5, 6, DateTimeKind.Utc);
        var unspecified = new DateTime(2031, 2, 3, 2, 5, 6, DateTimeKind.Unspecified);
        var expected = new DateTimeOffset(2031, 2, 3, 2, 5, 6, TimeSpan.Zero);

        DateTimeOffset?[] actual =
        [
            CreateReceiveContext(new TestHeaders((MessageHeaders.TransportSentTime, offset))).GetSentTime(),
            CreateReceiveContext(new TestHeaders((MessageHeaders.TransportSentTime, utc))).GetSentTime(),
            CreateReceiveContext(new TestHeaders((MessageHeaders.TransportSentTime, unspecified))).GetSentTime(),
            CreateReceiveContext(new TestHeaders((MessageHeaders.TransportSentTime, "2031-02-03T04:05:06+02:00"))).GetSentTime(),
        ];

        Assert.All(actual, timestamp =>
        {
            Assert.Equal(expected, timestamp);
            Assert.Equal(TimeSpan.Zero, timestamp?.Offset);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "named-context-readers-forward-exact-standard-headers")]
    public void NamedContextReaders_ExposeEveryStandardHeaderWithoutChangingItsMeaning()
    {
        Guid messageId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid correlationId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        Guid conversationId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        Guid requestId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        Guid initiatorId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        var source = new Uri("loopback://localhost/source");
        var response = new Uri("loopback://localhost/response");
        var fault = new Uri("loopback://localhost/fault");
        var headers = new TestHeaders(
            (MessageHeaders.MessageId, messageId),
            (MessageHeaders.CorrelationId, correlationId.ToString("D")),
            (MessageHeaders.ConversationId, conversationId),
            (MessageHeaders.RequestId, requestId.ToString("D")),
            (MessageHeaders.InitiatorId, initiatorId),
            (MessageHeaders.SourceAddress, source),
            (MessageHeaders.ResponseAddress, response.ToString()),
            (MessageHeaders.FaultAddress, fault),
            (MessageHeaders.MessageType, "urn:message:first;urn:message:second"));
        ReceiveContext context = CreateReceiveContext(headers);

        Assert.Equal(messageId, context.GetMessageId());
        Assert.Equal(messageId, context.GetMessageId(Guid.Empty));
        Assert.Equal(correlationId, context.GetCorrelationId());
        Assert.Equal(conversationId, context.GetConversationId());
        Assert.Equal(requestId, context.GetRequestId());
        Assert.Equal(initiatorId, context.GetInitiatorId());
        Assert.Equal(source, context.GetSourceAddress());
        Assert.Equal(response, context.GetResponseAddress());
        Assert.Equal(fault, context.GetFaultAddress());
        Assert.Equal(["urn:message:first", "urn:message:second"], context.GetMessageTypes());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "content-encoding")]
    public void ContentEncoding_UsesTheDefaultOrTheDeclaredEncoding()
    {
        var noEncoding = new TestHeaders();
        var declared = new TestHeaders(("Content-Encoding", "utf-16"));

        Assert.Same(MessageDefaults.Encoding, noEncoding.GetMessageEncoding());
        Assert.Equal(Encoding.Unicode.WebName, declared.GetMessageEncoding().WebName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "invalid-content-encoding-is-rejected")]
    public void ContentEncoding_RejectsAnUnknownDeclaredEncoding()
    {
        var headers = new TestHeaders(("Content-Encoding", "not-a-real-character-encoding"));

        Assert.Throws<ArgumentException>(() => headers.GetMessageEncoding());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "required-boundaries")]
    public void MetadataReaders_RejectMissingContextsHeadersAndKeys()
    {
        var headers = new TestHeaders();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => ReceiveContextExtensions.GetMessageId((ReceiveContext)null!)).ParamName);
        Assert.Equal(
            "headers",
            Assert.Throws<ArgumentNullException>(() => ReceiveContextExtensions.GetMessageId((Headers)null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => headers.GetHeaderId(" ")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => headers.GetEndpointAddress("")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-BODY", "explicit-byte-and-stream-access")]
    public void BodyReaders_ExposeBytesAndStreamsAndRejectMissingContexts()
    {
        byte[] expected = [1, 2, 3];
        ReceiveContext context = CreateReceiveContext(new TestHeaders(), new TestMessageBody(expected));

        Assert.Equal(expected, context.GetBodyBytes());
        using Stream stream = context.GetBodyStream();
        Assert.Equal(expected, ReadAll(stream));
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => ReceiveContextBodyExtensions.GetBodyBytes(null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => ReceiveContextBodyExtensions.GetBodyStream(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADER-COPY", "required-boundaries")]
    public void HeaderCopy_RejectsEveryMissingRequiredInput()
    {
        var headers = new TestHeaders();
        SendHeaders sendHeaders = DispatchProxy.Create<SendHeaders, UnusedProxy>();
        var destination = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();

        Assert.Equal(
            "sendHeaders",
            Assert.Throws<ArgumentNullException>(() => SendHeadersExtensions.CopyFrom(null!, headers)).ParamName);
        Assert.Equal(
            "headers",
            Assert.Throws<ArgumentNullException>(() => sendHeaders.CopyFrom(null!)).ParamName);
        Assert.Equal(
            "adapter",
            Assert.Throws<ArgumentNullException>(() => SendHeadersExtensions.CopyFrom<string>(null!, destination, headers)).ParamName);
        Assert.Equal(
            "sendHeaders",
            Assert.Throws<ArgumentNullException>(() => adapter.CopyFrom(null!, headers)).ParamName);
        Assert.Equal(
            "headers",
            Assert.Throws<ArgumentNullException>(() => adapter.CopyFrom(destination, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADER-COPY", "all-values-forwarded-to-both-destination-kinds")]
    public void HeaderCopy_ForwardsEveryNamedValueToBothDestinationKinds()
    {
        var source = new TestHeaders(("text", "value"), ("count", 42));
        var sendHeaders = new RecordingSendHeaders();
        var dictionary = new Dictionary<string, string>();
        var adapter = new StringHeaderAdapter();

        sendHeaders.CopyFrom(source);
        adapter.CopyFrom(dictionary, source);

        Assert.Equal("value", sendHeaders.Values["text"]);
        Assert.Equal(42, sendHeaders.Values["count"]);
        Assert.Equal("value", dictionary["text"]);
        Assert.Equal("42", dictionary["count"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADER-COPY", "adapter-public-generic-metadata")]
    public void HeaderAdapter_UsesDescriptiveGenericMetadata()
    {
        Type adapterType = typeof(ITransportSetHeaderAdapter<>);
        MethodInfo genericSet = adapterType.GetMethods().Single(method => method.IsGenericMethod);

        Assert.Equal("THeaderValue", Assert.Single(adapterType.GetGenericArguments()).Name);
        Assert.Equal("TValue", Assert.Single(genericSet.GetGenericArguments()).Name);
    }

    private static ReceiveContext CreateReceiveContext(Headers headers, MessageBody? body = null)
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)context).Initialize(headers, body);
        return context;
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var destination = new MemoryStream();
        stream.CopyTo(destination);
        return destination.ToArray();
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private Headers _headers = null!;
        private MessageBody? _body;

        public void Initialize(Headers headers, MessageBody? body)
        {
            _headers = headers;
            _body = body;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_TransportHeaders" => _headers,
                "get_Body" => _body ?? throw new InvalidOperationException("No body was configured for this test context."),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
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

    private sealed class TestMessageBody(byte[] bytes) : MessageBody
    {
        public long? Length => bytes.LongLength;

        public Stream GetStream() => new MemoryStream(bytes, writable: false);

        public byte[] GetBytes() => [.. bytes];

        public string GetString() => Encoding.UTF8.GetString(bytes);
    }

    private sealed class StringHeaderAdapter : ITransportSetHeaderAdapter<string>
    {
        public void Set(IDictionary<string, string> dictionary, in HeaderValue headerValue) =>
            dictionary[headerValue.Key] = headerValue.Value.ToString()!;

        public void Set<TValue>(IDictionary<string, string> dictionary, in HeaderValue<TValue> headerValue) =>
            dictionary[headerValue.Key] = headerValue.Value is null ? string.Empty : headerValue.Value.ToString()!;
    }

    private sealed class RecordingSendHeaders : SendHeaders
    {
        public Dictionary<string, object> Values { get; } = [];

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

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
