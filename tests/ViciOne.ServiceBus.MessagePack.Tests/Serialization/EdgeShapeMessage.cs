namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class EdgeShapeMessage(string privateValue)
{
    public string PrivateValue { get; private set; } = privateValue;

    public char Character { get; set; }

    public char? OptionalCharacter { get; set; }

    public ExampleState State { get; set; }
}
