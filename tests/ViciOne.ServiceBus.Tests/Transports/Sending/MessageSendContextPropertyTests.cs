using System.Net.Mime;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Sending;

public sealed class MessageSendContextPropertyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-CONTEXT", "construction-identity-and-cancellation")]
    public void Construction_RequiresAMessageAndCreatesStableIdentityMetadata()
    {
        using var source = new CancellationTokenSource();

        var context = new MessageSendContext<ProbeMessage>(new ProbeMessage(), source.Token);

        Assert.NotEqual(Guid.Empty, context.MessageId);
        Assert.NotNull(context.SentTime);
        Assert.Equal(source.Token, context.CancellationToken);
        Assert.Contains(MessageUrn.ForTypeString<ProbeMessage>(), context.SupportedMessageTypes);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new MessageSendContext<ProbeMessage>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-SERIALIZATION", "required-state-lazy-body-and-immutable-serializer")]
    public void SerializationState_IsRequiredAndTheBodyIsCreatedExactlyOnce()
    {
        var unconfigured = new MessageSendContext<ProbeMessage>(new ProbeMessage());
        Assert.Equal(
            "A message serializer has not been configured.",
            Assert.Throws<InvalidOperationException>(() => _ = unconfigured.Serializer).Message);
        Assert.Equal(
            "Serialization has not been configured.",
            Assert.Throws<InvalidOperationException>(() => _ = unconfigured.Serialization).Message);
        Assert.Equal(
            "Unable to serialize the message because no serializer is configured.",
            Assert.Throws<SerializationException>(() => _ = unconfigured.Body).Message);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => unconfigured.Serializer = null!).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => unconfigured.Serialization = null!).ParamName);

        ISerialization serialization = DispatchProxy.Create<ISerialization, UnexpectedInvocationProxy>();
        var serializer = new RecordingSerializer([1, 2, 3]);
        var configured = new MessageSendContext<ProbeMessage>(new ProbeMessage())
        {
            Serialization = serialization,
            Serializer = serializer,
        };

        Assert.Same(serialization, configured.Serialization);
        Assert.Same(serializer, configured.Serializer);
        Assert.Equal(serializer.ContentType, configured.ContentType);
        Assert.Null(configured.BodyLength);

        MessageBody body = configured.Body;
        Assert.Same(body, configured.Body);
        Assert.Equal(3, configured.BodyLength);
        Assert.Equal(1, serializer.InvocationCount);
        Assert.Equal(
            "The message was already serialized.",
            Assert.Throws<InvalidOperationException>(() => configured.Serializer = new RecordingSerializer([4])).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-METADATA", "stable-identities-before-serialization")]
    public void DurableAdmissionMetadata_ReplacesDispatchIdentityOnlyBeforeSerialization()
    {
        Guid idempotencyKey = Guid.Parse("d9308e7f-dcc3-44e2-ac52-e08c335b6694");
        Guid correlationId = Guid.Parse("67964562-72aa-4e28-8ceb-33142165eb22");
        var context = new MessageSendContext<ProbeMessage>(new ProbeMessage());

        context.SetDurableAdmissionMetadata(idempotencyKey, correlationId);

        Assert.Equal(idempotencyKey, context.MessageId);
        Assert.Equal(idempotencyKey, context.ConversationId);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Null(context.SentTime);

        context.Serializer = new RecordingSerializer([1]);
        _ = context.Body;
        Assert.Equal(
            "Durable admission metadata must be fixed before serialization.",
            Assert.Throws<InvalidOperationException>(() => context.SetDurableAdmissionMetadata(Guid.NewGuid(), null)).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-CONTEXT", "typed-proxy-preserves-owner-state")]
    public void TypedProxy_UsesTheReplacementMessageAndPreservesTheOwningContext()
    {
        var context = new MessageSendContext<ProbeMessage>(new ProbeMessage());
        var replacement = new DerivedProbeMessage();

        SendContext<DerivedProbeMessage> proxy = context.CreateProxy(replacement);

        Assert.Same(replacement, proxy.Message);
        Assert.Equal(context.MessageId, proxy.MessageId);
        Assert.Same(context.Headers, proxy.Headers);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => context.CreateProxy<DerivedProbeMessage>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-PROPERTIES", "round-trip-and-default-semantics")]
    public void TransportProperties_RoundTripNonDefaultsAndRestoreDefaultsWhenAbsent()
    {
        var source = new MessageSendContext<ProbeMessage>(new ProbeMessage())
        {
            Durable = false,
            Mandatory = true,
            Delay = TimeSpan.FromSeconds(3),
        };
        var properties = new Dictionary<string, object>();

        source.WritePropertiesTo(properties);

        var restored = new MessageSendContext<ProbeMessage>(new ProbeMessage());
        restored.ReadPropertiesFrom(properties);
        Assert.False(restored.Durable);
        Assert.True(restored.Mandatory);
        Assert.Equal(TimeSpan.FromSeconds(3), restored.Delay);

        restored.ReadPropertiesFrom(new Dictionary<string, object>());
        Assert.True(restored.Durable);
        Assert.False(restored.Mandatory);
        Assert.Null(restored.Delay);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(() => source.WritePropertiesTo(null!)).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(() => source.ReadPropertiesFrom(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-PROPERTIES", "numeric-narrowing-rejects-data-loss")]
    public void NumericReaders_RejectValuesThatCannotBeRepresentedWithoutDataLoss()
    {
        Assert.Equal(byte.MaxValue, PropertyReader.ReadByteValue(byte.MaxValue));
        Assert.Equal(int.MaxValue, PropertyReader.ReadIntValue(int.MaxValue));
        Assert.Equal(short.MinValue, PropertyReader.ReadShortValue(short.MinValue));

        Assert.Throws<OverflowException>(() => PropertyReader.ReadByteValue(-1L));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadByteValue((long)byte.MaxValue + 1));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadIntValue((long)int.MinValue - 1));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadIntValue((long)int.MaxValue + 1));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadShortValue((long)short.MinValue - 1));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadShortValue((long)short.MaxValue + 1));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadLongValue(ulong.MaxValue));
        Assert.Throws<OverflowException>(() => PropertyReader.ReadLongValue(nuint.MaxValue));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-PROPERTIES", "supported-native-text-and-utf8-representations")]
    public void PropertyReaders_DecodeEverySupportedRepresentationAndUseExplicitFallbacks()
    {
        Assert.Equal("native", PropertyReader.ReadStringValue("native"));
        Assert.Equal("utf8", PropertyReader.ReadStringValue(Encoding.UTF8.GetBytes("utf8")));
        Assert.Equal("fallback", PropertyReader.ReadStringValue(new object(), "fallback"));
        Assert.Equal(["one", "two"], PropertyReader.ReadStringArrayValue("one;two"));
        Assert.Equal(["three", "four"], PropertyReader.ReadStringArrayValue(Encoding.UTF8.GetBytes("three;four")));
        Assert.Empty(PropertyReader.ReadStringArrayValue(42));

        TimeSpan duration = TimeSpan.FromMinutes(17);
        Assert.Equal(duration, PropertyReader.ReadTimeSpanValue(duration));
        Assert.Equal(duration, PropertyReader.ReadTimeSpanValue(duration.ToString("c")));
        Assert.Equal(duration, PropertyReader.ReadTimeSpanValue(Encoding.UTF8.GetBytes(duration.ToString("c"))));
        Assert.Equal(TimeSpan.FromSeconds(9), PropertyReader.ReadTimeSpanValue("invalid", TimeSpan.FromSeconds(9)));

        Assert.Equal(PropertyKind.Second, PropertyReader.ReadEnumValue("second"));
        Assert.Equal(PropertyKind.First, PropertyReader.ReadEnumValue(Encoding.UTF8.GetBytes("FIRST")));
        Assert.Equal(PropertyKind.Second, PropertyReader.ReadEnumValue("invalid", PropertyKind.Second));

        Assert.Equal(-12, PropertyReader.ReadLongValue((sbyte)-12));
        Assert.Equal(250, PropertyReader.ReadLongValue((byte)250));
        Assert.Equal(short.MaxValue, PropertyReader.ReadLongValue(short.MaxValue));
        Assert.Equal(ushort.MaxValue, PropertyReader.ReadLongValue(ushort.MaxValue));
        Assert.Equal(int.MinValue, PropertyReader.ReadLongValue(int.MinValue));
        Assert.Equal(uint.MaxValue, PropertyReader.ReadLongValue(uint.MaxValue));
        Assert.Equal(long.MinValue, PropertyReader.ReadLongValue(long.MinValue));
        Assert.Equal(42, PropertyReader.ReadLongValue((nint)42));
        Assert.Equal(43, PropertyReader.ReadLongValue((nuint)43));
        Assert.Equal('A', PropertyReader.ReadLongValue('A'));
        Assert.Equal(44, PropertyReader.ReadLongValue("44"));
        Assert.Equal(45, PropertyReader.ReadLongValue(Encoding.UTF8.GetBytes("45")));
        Assert.Equal(91, PropertyReader.ReadLongValue("invalid", 91));
        Assert.Equal(255, PropertyReader.ReadByteValue("255"));

        Assert.True(PropertyReader.ReadBooleanValue(true));
        Assert.True(PropertyReader.ReadBooleanValue(-1));
        Assert.False(PropertyReader.ReadBooleanValue(0));
        Assert.True(PropertyReader.ReadBooleanValue("invalid", true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-SEND-PROPERTIES", "lookup-input-validation")]
    public void PropertyReaders_RejectMissingBagsAndBlankKeysConsistently()
    {
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadStringFromNullProperties).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(PropertyReader.ReadStringWithBlankKey).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadStringArrayFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadTimeSpanFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadEnumFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadByteFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadIntFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadShortFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadLongFromNullProperties).ParamName);
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(PropertyReader.ReadBooleanFromNullProperties).ParamName);
    }

    private record ProbeMessage;

    private sealed record DerivedProbeMessage : ProbeMessage;

    private enum PropertyKind
    {
        First,
        Second,
    }

    private sealed class RecordingSerializer(byte[] bytes) : IMessageSerializer
    {
        private readonly MessageBody _body = new BinaryMessageBody(bytes);

        public ContentType ContentType { get; } = new("application/vnd.vicione.context-test");

        public int InvocationCount { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class
        {
            InvocationCount++;
            return _body;
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Serialization state unexpectedly invoked {targetMethod?.Name}.");
    }

    private sealed class PropertyReader : MessageSendContext<ProbeMessage>
    {
        private const string Key = "Value";

        private PropertyReader()
            : base(new ProbeMessage())
        {
        }

        public static byte ReadByteValue(object value) => ReadByte(CreateProperties(value), Key);

        public static int? ReadIntValue(object value) => ReadInt(CreateProperties(value), Key);

        public static short? ReadShortValue(object value) => ReadShort(CreateProperties(value), Key);

        public static string? ReadStringValue(object value, string? defaultValue = null) => ReadString(CreateProperties(value), Key, defaultValue);

        public static string[] ReadStringArrayValue(object value) => ReadStringArray(CreateProperties(value), Key);

        public static TimeSpan? ReadTimeSpanValue(object value, TimeSpan? defaultValue = null) =>
            ReadTimeSpan(CreateProperties(value), Key, defaultValue);

        public static PropertyKind? ReadEnumValue(object value, PropertyKind? defaultValue = null) =>
            ReadEnum(CreateProperties(value), Key, defaultValue);

        public static long? ReadLongValue(object value, long? defaultValue = null) => ReadLong(CreateProperties(value), Key, defaultValue);

        public static bool ReadBooleanValue(object value, bool defaultValue = false) => ReadBoolean(CreateProperties(value), Key, defaultValue);

        public static void ReadStringFromNullProperties() => ReadString(null!, Key);

        public static void ReadStringWithBlankKey() => ReadString(CreateProperties("value"), " ");

        public static void ReadStringArrayFromNullProperties() => ReadStringArray(null!, Key);

        public static void ReadTimeSpanFromNullProperties() => ReadTimeSpan(null!, Key);

        public static void ReadEnumFromNullProperties() => ReadEnum<PropertyKind>(null!, Key);

        public static void ReadByteFromNullProperties() => ReadByte(null!, Key);

        public static void ReadIntFromNullProperties() => ReadInt(null!, Key);

        public static void ReadShortFromNullProperties() => ReadShort(null!, Key);

        public static void ReadLongFromNullProperties() => ReadLong(null!, Key);

        public static void ReadBooleanFromNullProperties() => ReadBoolean(null!, Key);

        private static IReadOnlyDictionary<string, object> CreateProperties(object value) =>
            new Dictionary<string, object> { [Key] = value };
    }
}
