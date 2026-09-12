using MessagePack;
using MessagePack.Resolvers;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackEnvelopeTests
{
    private static readonly MessagePackSerializerOptions ExternalOracleOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ENVELOPE", "binary-only-payload-contract")]
    public void EnvelopePayload_IsStaticallyRestrictedToOwnedBinaryContent()
    {
        var property = typeof(MessagePackEnvelope).GetProperty(nameof(MessagePackEnvelope.Message));
        var message = new Order { Id = 27, Customer = "Frank" };
        var sendContext = new MessageSendContext<Order>(message);
        var envelope = new MessagePackEnvelope(sendContext, message);
        MessageEnvelope contract = envelope;

        Assert.NotNull(property);
        Assert.Equal(typeof(byte[]), property.PropertyType);
        Assert.Same(envelope.Message, contract.Message);
        Assert.NotNull(contract.MessageTypes);
        Assert.NotNull(contract.Headers);

        envelope.MessageTypes = null;
        envelope.Headers = null;

        Assert.Null(contract.MessageTypes);
        Assert.Null(contract.Headers);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ENVELOPE", "complete-constructor-boundary")]
    public void EnvelopeConstruction_RejectsEveryMissingOwnedInput()
    {
        var message = new Order { Id = 27, Customer = "Frank" };
        var sendContext = new MessageSendContext<Order>(message);
        var messageContext = new TestMessageContext();
        var envelope = new MessagePackEnvelope(sendContext, message);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope((SendContext)null!, message)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope(sendContext, (object)null!)).ParamName);
        Assert.Equal("serializedMessage", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope(sendContext, (byte[])null!)).ParamName);
        Assert.Equal("envelope", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope((MessageEnvelope)null!)).ParamName);
        Assert.Equal("serializedMessage", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope(envelope, null!, isNativeMessagePackPayload: true)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope((MessageContext)null!, message, [])).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope(messageContext, null!, [])).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackEnvelope(messageContext, message, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            envelope.Update<Order>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ENVELOPE", "serialized-payload-snapshot")]
    public void SerializedPayloadConstruction_OwnsItsInputBytes()
    {
        var message = new Order { Id = 27, Customer = "Frank" };
        var context = new MessageSendContext<Order>(message);
        byte[] source = MessagePackSerializationRuntime.Serialize(message);
        byte[] expected = [.. source];

        var envelope = new MessagePackEnvelope(context, source);
        source.AsSpan().Fill(0x00);

        Assert.NotSame(source, envelope.Message);
        Assert.Equal(expected, envelope.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CLONE", "native-payload-byte-identity")]
    public void NativeEnvelopeClone_PreservesPayloadBytesWithoutAliasing()
    {
        var source = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));
        var sourceBytes = Assert.IsType<byte[]>(source.Message);
        byte[] expectedBytes = [.. sourceBytes];

        var clone = new MessagePackEnvelope(source);
        var cloneBytes = Assert.IsType<byte[]>(clone.Message);
        sourceBytes[0] ^= 0xFF;

        Assert.True(clone.IsNativeMessagePackPayload);
        Assert.NotSame(sourceBytes, cloneBytes);
        Assert.Equal(expectedBytes, cloneBytes);
        var roundTrip = MessagePackSerializer.Deserialize<Order>(
            cloneBytes,
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);
        Assert.Equal(27, roundTrip.Id);
        Assert.Equal("Frank", roundTrip.Customer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CLONE", "repeated-clone-remains-readable")]
    public void RepeatedClone_DoesNotAddEncodingLayers()
    {
        var clone = new MessagePackEnvelope(
            new MessagePackEnvelope(
                new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }))));

        var roundTrip = MessagePackSerializer.Deserialize<Order>(
            Assert.IsType<byte[]>(clone.Message),
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);

        Assert.Equal(27, roundTrip.Id);
        Assert.Equal("Frank", roundTrip.Customer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CLONE", "overlay-form-preserved")]
    public void OverlayEnvelopeClone_PreservesDictionaryFormAndNativeFlag()
    {
        var source = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }))
        {
            IsNativeMessagePackPayload = false,
            Message = MessagePackSerializer.Serialize(
                new Dictionary<string, object> { ["id"] = 27 },
                ExternalOracleOptions,
                TestContext.Current.CancellationToken),
        };
        var sourceBytes = Assert.IsType<byte[]>(source.Message);

        var clone = new MessagePackEnvelope(source);

        Assert.False(clone.IsNativeMessagePackPayload);
        Assert.Equal(sourceBytes, Assert.IsType<byte[]>(clone.Message));
        Assert.NotSame(sourceBytes, clone.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CLONE", "foreign-envelope-encoded")]
    public void ForeignEnvelopeClone_EncodesTheForeignMessageOnce()
    {
        var clone = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));

        Assert.True(clone.IsNativeMessagePackPayload);
        var roundTrip = MessagePackSerializer.Deserialize<Order>(
            Assert.IsType<byte[]>(clone.Message),
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);
        Assert.Equal(27, roundTrip.Id);
        Assert.Equal("Frank", roundTrip.Customer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CLONE", "metadata-copied-without-aliasing")]
    public void Clone_CopiesMetadataAndOwnsItsHeaderDictionary()
    {
        var source = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }))
        {
            MessageId = Guid.NewGuid().ToString(),
        };
        source.Headers!["Baggage"] = "kept";

        var clone = new MessagePackEnvelope(source);
        source.Headers["Baggage"] = "changed";

        Assert.Equal(source.MessageId, clone.MessageId);
        Assert.Equal("kept", clone.Headers!["Baggage"]);
        Assert.NotSame(source.Headers, clone.Headers);
    }

    private static ForeignEnvelope Foreign(object message) => new(message);

    private sealed class TestMessageContext : BasePipeContext, MessageContext
    {
        private readonly DictionarySendHeaders _headers = new();

        public Guid? MessageId => null;
        public Guid? RequestId => null;
        public Guid? CorrelationId => null;
        public Guid? ConversationId => null;
        public Guid? InitiatorId => null;
        public DateTimeOffset? ExpirationTime => null;
        public Uri? SourceAddress => null;
        public Uri? DestinationAddress => null;
        public Uri? ResponseAddress => null;
        public Uri? FaultAddress => null;
        public DateTimeOffset? SentTime => null;
        public Headers Headers => _headers;
        public HostInfo Host => HostMetadataCache.Host;
    }

    private sealed class ForeignEnvelope(object message) : MessageEnvelope
    {
        public string? MessageId { get; } = Guid.NewGuid().ToString();

        public string? RequestId => null;

        public string? CorrelationId => null;

        public string? ConversationId => null;

        public string? InitiatorId => null;

        public string? SourceAddress => null;

        public string? DestinationAddress => null;

        public string? ResponseAddress => null;

        public string? FaultAddress => null;

        public IReadOnlyList<string> MessageTypes { get; } = ["urn:message:Order"];

        public object Message { get; } = message;

        public DateTimeOffset? ExpirationTime => null;

        public DateTimeOffset? SentTime { get; } = TimeProvider.System.GetUtcNow();

        public IReadOnlyDictionary<string, object?> Headers { get; } =
            new Dictionary<string, object?>();

        public HostInfo? Host => null;
    }

    public sealed class Order
    {
        public int Id { get; set; }

        public string Customer { get; set; } = string.Empty;
    }
}
