using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class MessageSendContextPropertyTests
{
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
    }

    private sealed record ProbeMessage;

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

        private static IReadOnlyDictionary<string, object> CreateProperties(object value) =>
            new Dictionary<string, object> { [Key] = value };
    }
}
