using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackSerializerContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "outer-envelope-failure-is-observable")]
    public void Deserialize_RejectsMalformedEnvelopeBytes()
    {
        var serializer = new MessagePackMessageSerializer();

        Assert.Throws<MessagePackSerializationException>(() =>
            serializer.Deserialize(new BytesMessageBody([0xC1]), EmptyHeaders.Instance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "missing-arguments-have-exact-ownership")]
    public void Deserialize_RejectsMissingArgumentsWithExactOwnership()
    {
        var serializer = new MessagePackMessageSerializer();

        Assert.Equal("receiveContext", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize((ReceiveContext)null!)).ParamName);
        Assert.Equal("body", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize(null!, EmptyHeaders.Instance)).ParamName);
        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize(new BytesMessageBody([]), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "unsupported-and-malformed-contracts-return-false")]
    public void TryGetMessage_ReturnsFalseForUnsupportedAndMalformedContracts()
    {
        RoundTripResult<ContextValue> roundTrip = MessagePackRoundTrip.ExecuteWithContext(
            new ContextValue { Id = 27, Name = "supported" });

        Assert.False(roundTrip.Context.TryGetMessage<OtherContextValue>(out var unsupported));
        Assert.Null(unsupported);
        Assert.False(roundTrip.Context.TryGetMessage(typeof(OtherContextValue), out var runtimeUnsupported));
        Assert.Null(runtimeUnsupported);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            roundTrip.Context.TryGetMessage(null!, out _)).ParamName);

        SerializerContext malformed = CreateContextWithPayload([0xC1]);
        Assert.False(malformed.TryGetMessage<ContextValue>(out var invalid));
        Assert.Null(invalid);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "non-null-envelope-payload")]
    public void SerializerContext_RejectsAnEnvelopeWithoutAPayload()
    {
        var serializer = new MessagePackMessageSerializer();
        var sendContext = new MessageSendContext<ContextValue>(new ContextValue());
        var envelope = new MessagePackEnvelope(sendContext, sendContext.Message) { Message = null };
        var messageContext = new EnvelopeMessageContext(envelope, serializer);

        var exception = Assert.Throws<ArgumentException>(() =>
            new MessagePackSerializerContext(serializer, messageContext, [], envelope));

        Assert.Equal("envelope", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "dictionary-and-explicit-contract-projection")]
    public void SerializerContext_ProjectsDictionariesAndExplicitContractSets()
    {
        var message = new ContextValue { Id = 27, Name = "Frank" };
        RoundTripResult<ContextValue> source = MessagePackRoundTrip.ExecuteWithContext(message);

        Dictionary<string, object> values = source.Context.ToDictionary(message);
        Dictionary<string, object> empty = source.Context.ToDictionary<ContextValue>(null);
        IMessageSerializer forwarding = source.Context.GetMessageSerializer(
            message,
            [MessageUrn.ForTypeString<ContextValue>(), "urn:message:explicit-context-value"]);
        var sendContext = new MessageSendContext<ContextValue>(message);
        MessageBody body = forwarding.GetMessageBody(sendContext);
        SerializerContext result = new MessagePackMessageSerializer()
            .Deserialize(body, EmptyHeaders.Instance);

        Assert.Equal(27, source.Context.DeserializeObject<int>(values["ID"]));
        Assert.Equal("Frank", values["name"]);
        Assert.Empty(empty);
        Assert.Contains("urn:message:explicit-context-value", result.SupportedMessageTypes);
        Assert.True(result.TryGetMessage<ContextValue>(out var restored));
        Assert.Equal(27, restored.Id);
        Assert.Equal("Frank", restored.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "typed-overlay-preserves-existing-properties")]
    public void TypedForwardingSerializer_OverlaysReplacementValuesAndPreservesTheRemainingPayload()
    {
        var original = new ContextValue { Id = 27, Name = "original" };
        var originalSendContext = new MessageSendContext<ContextValue>(original);
        var envelope = new MessagePackEnvelope(originalSendContext, original);
        SerializerContext source = new MessagePackMessageSerializer().Deserialize(
            new MessagePackMessageBody<ContextValue>(originalSendContext, envelope),
            EmptyHeaders.Instance);
        IMessageSerializer forwarding = source.GetMessageSerializer(
            envelope,
            new ContextNameOverlay { Name = "updated" });

        var forwardedSendContext = new MessageSendContext<ContextValue>(original);
        SerializerContext result = new MessagePackMessageSerializer().Deserialize(
            forwarding.GetMessageBody(forwardedSendContext),
            EmptyHeaders.Instance);

        Assert.True(result.TryGetMessage<ContextValue>(out var restored));
        Assert.Equal(27, restored.Id);
        Assert.Equal("updated", restored.Name);
    }

    private static SerializerContext CreateContextWithPayload(byte[] payload)
    {
        var serializer = new MessagePackMessageSerializer();
        var sendContext = new MessageSendContext<ContextValue>(new ContextValue());
        var envelope = new MessagePackEnvelope(sendContext, sendContext.Message)
        {
            IsNativeMessagePackPayload = true,
            Message = payload,
        };
        byte[] envelopeBytes = MessagePackSerializationRuntime.Serialize(envelope);

        return serializer.Deserialize(new BytesMessageBody(envelopeBytes), EmptyHeaders.Instance);
    }

    private sealed class ContextValue
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class OtherContextValue;

    private sealed class ContextNameOverlay
    {
        public string Name { get; set; } = string.Empty;
    }
}
