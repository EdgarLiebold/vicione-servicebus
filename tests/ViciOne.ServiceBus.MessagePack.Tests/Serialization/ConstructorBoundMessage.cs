namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class ConstructorBoundMessage(string name, string value)
{
    public string Name { get; private set; } = name;

    public string Value { get; private set; } = value;
}
