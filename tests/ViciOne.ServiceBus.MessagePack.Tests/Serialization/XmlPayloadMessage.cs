namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class XmlPayloadMessage
{
    public string Text { get; set; } = string.Empty;

    public byte[] Bytes { get; set; } = [];
}
