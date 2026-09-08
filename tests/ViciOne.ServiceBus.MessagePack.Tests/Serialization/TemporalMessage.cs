namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class TemporalMessage
{
    public DateTime Local { get; set; }

    public DateTime Universal { get; set; }
}
