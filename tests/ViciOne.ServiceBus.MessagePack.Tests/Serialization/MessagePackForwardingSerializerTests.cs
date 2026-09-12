using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackForwardingSerializerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "owned-input-boundaries")]
    public void ForwardingSerializer_RejectsEveryMissingOwnedInput()
    {
        Assert.Equal("envelope", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackForwardingSerializer(null!)).ParamName);

        var sourceContext = CreateContext(
            new ForwardedMessage(),
            Guid.Parse("0dac5e86-7645-45b6-a3ad-1d22df1854c8"));
        var forwarding = new MessagePackForwardingSerializer(
            new MessagePackEnvelope(sourceContext, sourceContext.Message));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            forwarding.GetMessageBody<ForwardedMessage>(null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            forwarding.Overlay<ForwardingOverlay>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "reusable-serializer-isolates-eager-owned-bodies")]
    public void ReusedForwardingSerializer_IsolatesEveryEagerOwnedBody()
    {
        var sourceContext = CreateContext(
            new ForwardedMessage { Changed = "original", Preserved = "kept" },
            Guid.Parse("358ff6cf-d8d2-4c75-9bb8-6984b35b2b31"));
        var forwarding = new MessagePackForwardingSerializer(
            new MessagePackEnvelope(sourceContext, sourceContext.Message));
        var firstContext = CreateContext(
            sourceContext.Message,
            Guid.Parse("0ded4396-c01b-4b9b-a639-0047c75cc2bb"));
        var secondContext = CreateContext(
            sourceContext.Message,
            Guid.Parse("28647440-d837-48b5-914c-3869eeb4f0c8"));

        MessageBody firstBody = forwarding.GetMessageBody(firstContext);
        MessageBody secondBody = forwarding.GetMessageBody(secondContext);

        var serializer = new MessagePackMessageSerializer();
        SerializerContext first = serializer.Deserialize(firstBody, EmptyHeaders.Instance);
        SerializerContext second = serializer.Deserialize(secondBody, EmptyHeaders.Instance);

        Assert.Equal(firstContext.CorrelationId, first.CorrelationId);
        Assert.Equal(secondContext.CorrelationId, second.CorrelationId);
        Assert.True(first.TryGetMessage<ForwardedMessage>(out var firstMessage));
        Assert.True(second.TryGetMessage<ForwardedMessage>(out var secondMessage));
        Assert.Equal("original", firstMessage.Changed);
        Assert.Equal("kept", firstMessage.Preserved);
        Assert.Equal("original", secondMessage.Changed);
        Assert.Equal("kept", secondMessage.Preserved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "overlay-survives-payload-admission")]
    public void OverlayPayload_RemainsReadableWhenPayloadAdmissionIsActive()
    {
        var sourceContext = CreateContext(
            new ForwardedMessage { Changed = "original", Preserved = "kept" },
            Guid.Parse("c03f634d-8e44-4816-a1a8-21a31fb4fcb4"));
        var forwarding = new MessagePackForwardingSerializer(
            new MessagePackEnvelope(sourceContext, sourceContext.Message));
        forwarding.Overlay(new ForwardingOverlay { Changed = "updated" });
        var sendContext = CreateContext(
            sourceContext.Message,
            Guid.Parse("93b324e7-68b2-490d-8f9d-1d66a458077b"));
        AttachPayloadAdmission(sendContext);

        MessageBody body = forwarding.GetMessageBody(sendContext);
        MessagePackEnvelope envelope = MessagePackSerializationRuntime.Deserialize<MessagePackEnvelope>(body.ToArray());
        SerializerContext serializerContext = new MessagePackMessageSerializer()
            .Deserialize(body, EmptyHeaders.Instance);

        Assert.False(envelope.IsNativeMessagePackPayload);
        Assert.True(serializerContext.TryGetMessage<ForwardedMessage>(out var message));
        Assert.Equal("updated", message.Changed);
        Assert.Equal("kept", message.Preserved);
    }

    private static readonly string[] OverlayItems = ["added"];

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "recursive-case-insensitive-overlay-parity")]
    public void Overlay_MatchesNamesCaseInsensitivelyAndRecursivelyMergesObjectsAndArrays()
    {
        var sourceContext = CreateContext(
            new ForwardedMessage
            {
                Changed = "original",
                Preserved = "kept",
                Optional = "retain",
                Details = new ForwardedDetails { Changed = "old", Preserved = "nested-kept" },
                Items = ["original"],
            },
            Guid.Parse("b0a52f1c-0e79-46ae-9fea-d202f5e185d7"));
        var forwarding = new MessagePackForwardingSerializer(
            new MessagePackEnvelope(sourceContext, sourceContext.Message));
        forwarding.Overlay(new Dictionary<string, object?>
        {
            ["optional"] = null,
            ["details"] = new Dictionary<string, object?>
            {
                ["changed"] = "updated",
                ["preserved"] = null,
            },
            ["items"] = OverlayItems,
        });
        var sendContext = CreateContext(
            sourceContext.Message,
            Guid.Parse("1a8bc30c-18f0-45d9-b970-a071beb7937c"));

        SerializerContext serializerContext = new MessagePackMessageSerializer()
            .Deserialize(forwarding.GetMessageBody(sendContext), EmptyHeaders.Instance);

        Assert.True(serializerContext.TryGetMessage<ForwardedMessage>(out var message));
        Assert.Equal("retain", message.Optional);
        Assert.Equal("updated", message.Details.Changed);
        Assert.Equal("nested-kept", message.Details.Preserved);
        Assert.Equal(["original", "added"], message.Items);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "overlay-creates-missing-payload")]
    public void Overlay_CreatesAPayloadWhenTheEnvelopeHasNoMessage()
    {
        var sourceContext = CreateContext(
            new ForwardedMessage(),
            Guid.Parse("5b86dc4d-26d6-4cea-b0ac-7fb1bf9dc2e5"));
        var envelope = new MessagePackEnvelope(sourceContext, sourceContext.Message)
        {
            Message = null,
        };
        var forwarding = new MessagePackForwardingSerializer(envelope);
        forwarding.Overlay(new ForwardingOverlay { Changed = "created" });

        SerializerContext serializerContext = new MessagePackMessageSerializer().Deserialize(
            forwarding.GetMessageBody(sourceContext),
            EmptyHeaders.Instance);

        Assert.True(serializerContext.TryGetMessage<ForwardedMessage>(out var message));
        Assert.Equal("created", message.Changed);
    }

    private static MessageSendContext<ForwardedMessage> CreateContext(
        ForwardedMessage message,
        Guid correlationId) =>
        new(message)
        {
            CorrelationId = correlationId,
            DestinationAddress = new Uri("loopback://messagepack-forwarding/input"),
        };

    private static void AttachPayloadAdmission(SendContext context)
    {
        var policy = new PayloadAdmissionPolicy
        {
            MaximumSerializedBodyBytes = 1_000_000,
            MaximumTransportEnvelopeBytes = 1_000_000,
        };
        var runtime = new PayloadAdmissionRuntime<IBus>(
            new PayloadAdmissionEvaluator<IBus>(policy));
        context.GetOrAddPayload(
            () => new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved: false));
    }

    private sealed class ForwardedMessage
    {
        public string Changed { get; set; } = string.Empty;

        public string Preserved { get; set; } = string.Empty;

        public string? Optional { get; set; }

        public ForwardedDetails Details { get; set; } = new();

        public string[] Items { get; set; } = [];
    }

    private sealed class ForwardingOverlay
    {
        public string Changed { get; set; } = string.Empty;
    }

    private sealed class ForwardedDetails
    {
        public string Changed { get; set; } = string.Empty;

        public string Preserved { get; set; } = string.Empty;
    }
}
