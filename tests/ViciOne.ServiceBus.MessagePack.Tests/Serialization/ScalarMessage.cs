namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class ScalarMessage
{
    public decimal DecimalValue { get; set; }

    public long LongValue { get; set; }

    public bool BoolValue { get; set; }

    public byte ByteValue { get; set; }

    public int IntValue { get; set; }

    public DateTime DateTimeValue { get; set; }

    public TimeSpan TimeSpanValue { get; set; }

    public Guid GuidValue { get; set; }

    public string StringValue { get; set; } = string.Empty;

    public double DoubleValue { get; set; }

    public decimal? OptionalDecimal { get; set; }
}
