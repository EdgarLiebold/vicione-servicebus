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
    [RequirementCoverage("REQ-VSB-RECEIVE-METADATA", "content-encoding")]
    public void ContentEncoding_UsesTheDefaultOrTheDeclaredEncoding()
    {
        var noEncoding = new TestHeaders();
        var declared = new TestHeaders(("Content-Encoding", "utf-16"));

        Assert.Same(MessageDefaults.Encoding, noEncoding.GetMessageEncoding());
        Assert.Equal(Encoding.Unicode.WebName, declared.GetMessageEncoding().WebName);
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

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
