using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator.Contexts;

public sealed class MediatorSerializationContextContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT", "typed-and-runtime-contract-projection")]
    public void TryGetMessage_ReturnsTheMaterializedInstanceOnlyForImplementedContracts()
    {
        var message = new SerializationMessage("value", 42);
        MediatorSerializationContext<SerializationMessage> context = CreateContext(message);

        Assert.True(context.TryGetMessage<SerializationMessage>(out SerializationMessage? concrete));
        Assert.Same(message, concrete);
        Assert.True(context.TryGetMessage<ISerializationContract>(out ISerializationContract? contract));
        Assert.Same(message, contract);
        Assert.False(context.TryGetMessage<UnrelatedMessage>(out UnrelatedMessage? unrelated));
        Assert.Null(unrelated);
        Assert.True(context.TryGetMessage(typeof(ISerializationContract), out object? runtimeContract));
        Assert.Same(message, runtimeContract);
        Assert.False(context.TryGetMessage(typeof(UnrelatedMessage), out object? runtimeUnrelated));
        Assert.Null(runtimeUnrelated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT", "runtime-type-and-message-boundaries")]
    public void Context_RejectsNullMessageAndRuntimeType()
    {
        MessageContext metadata = CreateMetadata();

        ArgumentNullException messageFailure = Assert.Throws<ArgumentNullException>(() =>
            new MediatorSerializationContext<SerializationMessage>(
                ServiceBusMetadataJson.ObjectDeserializer,
                metadata,
                null!,
                [MessageUrn.ForTypeString<SerializationMessage>()]));
        MediatorSerializationContext<SerializationMessage> context = CreateContext(new SerializationMessage("valid", 2));
        ArgumentNullException typeFailure = Assert.Throws<ArgumentNullException>(() =>
            context.TryGetMessage(null!, out _));

        Assert.Equal("message", messageFailure.ParamName);
        Assert.Equal("messageType", typeFailure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT", "initializer-dictionary-projection")]
    public void ToDictionary_ProjectsTheCurrentContractValues()
    {
        var message = new SerializationMessage("dictionary", 73);
        MediatorSerializationContext<SerializationMessage> context = CreateContext(message);

        Dictionary<string, object> values = context.ToDictionary(message);

        Assert.Equal(2, values.Count);
        Assert.Equal("dictionary", values["text"]);
        Assert.Equal(73, values["number"]);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            context.ToDictionary<SerializationMessage>(null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SERIALIZATION-CONTEXT", "transport-serializer-is-unavailable")]
    public void SerializerAccessors_RejectTransportSerializationConsistently()
    {
        var message = new SerializationMessage("unsupported", 3);
        MediatorSerializationContext<SerializationMessage> context = CreateContext(message);

        NotSupportedException untyped = Assert.Throws<NotSupportedException>(() => context.GetMessageSerializer());
        NotSupportedException typed = Assert.Throws<NotSupportedException>(() =>
            context.GetMessageSerializer<SerializationMessage>(null!, message));
        NotSupportedException runtime = Assert.Throws<NotSupportedException>(() =>
            context.GetMessageSerializer(message, [MessageUrn.ForTypeString<SerializationMessage>()]));

        Assert.Equal("The in-process mediator does not expose a transport message serializer.", untyped.Message);
        Assert.Equal(untyped.Message, typed.Message);
        Assert.Equal(untyped.Message, runtime.Message);
    }

    private static MediatorSerializationContext<SerializationMessage> CreateContext(SerializationMessage message) =>
        new(
            ServiceBusMetadataJson.ObjectDeserializer,
            CreateMetadata(),
            message,
            [MessageUrn.ForTypeString<SerializationMessage>(), MessageUrn.ForTypeString<ISerializationContract>()]);

    private static MessageContext CreateMetadata() => new TestMessageContext();

    private sealed class TestMessageContext : MessageContext
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

    private interface ISerializationContract
    {
        string Text { get; }
        int Number { get; }
    }

    private sealed record SerializationMessage(string Text, int Number) : ISerializationContract;
    private sealed record UnrelatedMessage(string Value);
}
