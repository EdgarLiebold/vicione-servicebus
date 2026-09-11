using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SerializerContextContractTests
{
    private static readonly string MessageType = MessageUrn.ForTypeString<ContextMessage>();

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT", "native-message-and-contract-snapshot")]
    public void NativeMessage_IsReturnedDirectlyAndContractNamesAreDefensiveSnapshots()
    {
        var message = new ContextMessage(27);
        string[] messageTypes = [MessageType];
        var context = CreateContext(message, messageTypes);
        messageTypes[0] = "mutated-input";

        Assert.True(context.TryGetMessage<ContextMessage>(out ContextMessage? restored));
        Assert.Same(message, restored);
        Assert.Equal([MessageType], context.SupportedMessageTypes);

        string[] exposed = context.SupportedMessageTypes;
        exposed[0] = "mutated-output";

        Assert.Equal([MessageType], context.SupportedMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT", "native-message-runtime-contract")]
    public void NativeMessage_IsReturnedThroughTheRuntimeContractPath()
    {
        var message = new ContextMessage(27);
        var context = CreateContext(message, [MessageType]);

        Assert.True(context.TryGetMessage(typeof(ContextMessage), out object? restored));
        Assert.Same(message, restored);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT", "metadata-codec-delegation")]
    public void MetadataCodec_SerializesAndDeserializesReferenceAndValueTypes()
    {
        var context = CreateContext(new ContextMessage(27), [MessageType]);

        MessageBody body = context.SerializeObject(new ContextMessage(73));
        ContextMessage? reference = context.DeserializeObject<ContextMessage>(body.GetString());
        int? value = context.DeserializeObject<int>("42");

        Assert.Equal(new ContextMessage(73), reference);
        Assert.Equal(42, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-SERIALIZER-CONTEXT", "valid-contract-names")]
    public void Constructor_RejectsAnInvalidMessageTypeName(string? messageType)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateContext(new ContextMessage(27), [messageType!]));

        Assert.Equal("supportedMessageTypes", exception.ParamName);
    }

    private static SystemTextJsonSerializerContext CreateContext(object message, string[] messageTypes)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        return new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            messageTypes,
            message: message);
    }

    private sealed record ContextMessage(int Value);
}
