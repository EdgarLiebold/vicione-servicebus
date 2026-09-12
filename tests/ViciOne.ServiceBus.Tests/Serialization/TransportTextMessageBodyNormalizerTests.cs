using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class TransportTextMessageBodyNormalizerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-TEXT-NORMALIZATION", "required-body-and-deserializer-boundaries")]
    public void Normalize_RejectsMissingDependenciesWithExactOwnership()
    {
        IMessageDeserializer deserializer = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);
        var body = new StringMessageBody("{}");

        Assert.Equal(
            "body",
            Assert.Throws<ArgumentNullException>(() => TransportTextMessageBodyNormalizer.Normalize(null!, deserializer)).ParamName);
        Assert.Equal(
            "deserializer",
            Assert.Throws<ArgumentNullException>(() => TransportTextMessageBodyNormalizer.Normalize(body, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-TEXT-NORMALIZATION", "payload-text-is-converted-exactly-once")]
    public void Normalize_WithPayloadText_ReturnsTheExactDeserializerBodyAfterOneConversion()
    {
        var expected = new StringMessageBody("normalized");
        var deserializer = new TrackingDeserializer(expected);
        var body = new OptionalTextBody("native-carrier", "serializer-payload");

        MessageBody actual = TransportTextMessageBodyNormalizer.Normalize(body, deserializer);

        Assert.Same(expected, actual);
        Assert.Equal(1, deserializer.CallCount);
        Assert.Equal("serializer-payload", deserializer.ObservedText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-TEXT-NORMALIZATION", "binary-body-is-preserved-without-conversion")]
    public void Normalize_WithBinaryBody_ReturnsTheSameBodyWithoutCallingTheDeserializer()
    {
        var body = new BinaryMessageBody(new byte[] { 0x00, 0x80, 0xff });
        var deserializer = new TrackingDeserializer(new StringMessageBody("unused"));

        MessageBody actual = TransportTextMessageBodyNormalizer.Normalize(body, deserializer);

        Assert.Same(body, actual);
        Assert.Equal(0, deserializer.CallCount);
        Assert.Null(deserializer.ObservedText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-TEXT-NORMALIZATION", "text-body-without-payload-is-preserved")]
    public void Normalize_WithoutPayloadText_ReturnsTheSameBodyWithoutCallingTheDeserializer()
    {
        var body = new OptionalTextBody("native-carrier", null);
        var deserializer = new TrackingDeserializer(new StringMessageBody("unused"));

        MessageBody actual = TransportTextMessageBodyNormalizer.Normalize(body, deserializer);

        Assert.Same(body, actual);
        Assert.Equal(0, deserializer.CallCount);
        Assert.Null(deserializer.ObservedText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-TEXT-NORMALIZATION", "null-deserializer-result-is-rejected")]
    public void Normalize_WhenDeserializerReturnsNull_ThrowsAnExactContractFailure()
    {
        var body = new OptionalTextBody("native-carrier", "serializer-payload");
        var deserializer = new TrackingDeserializer(null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            TransportTextMessageBodyNormalizer.Normalize(body, deserializer));

        Assert.Equal(
            "The selected deserializer returned no message body for the transport text payload.",
            exception.Message);
        Assert.Equal(1, deserializer.CallCount);
        Assert.Equal("serializer-payload", deserializer.ObservedText);
    }

    sealed class OptionalTextBody : MessageBody, TransportTextMessageBody
    {
        readonly string? _payloadText;
        readonly string _transportText;

        public OptionalTextBody(string transportText, string? payloadText)
        {
            _transportText = transportText;
            _payloadText = payloadText;
        }

        public long Length => Encoding.UTF8.GetByteCount(_transportText);

        public byte[] ToArray() => Encoding.UTF8.GetBytes(_transportText);

        public Stream OpenReadStream() => new MemoryStream(ToArray(), false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            text = _transportText;
            return true;
        }

        public bool TryGetPayloadText([NotNullWhen(true)] out string? text)
        {
            text = _payloadText;
            return text is not null;
        }
    }

    sealed class TrackingDeserializer : IMessageDeserializer
    {
        readonly MessageBody? _result;

        public TrackingDeserializer(MessageBody? result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public string? ObservedText { get; private set; }

        public ContentType ContentType => new("application/vnd.test");

        public ConsumeContext Deserialize(ReceiveContext receiveContext) => throw new NotSupportedException();

        public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null) =>
            throw new NotSupportedException();

        public MessageBody GetMessageBody(string text)
        {
            CallCount++;
            ObservedText = text;
            return _result!;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }
}
