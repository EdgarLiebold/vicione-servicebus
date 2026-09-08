namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class ObjectGraphMessage
{
    public NestedMessage Nested { get; set; } = new();

    public List<NestedMessage> List { get; set; } = [];

    public NestedMessage[] Array { get; set; } = [];

    public KeyValuePair<string, string>[] Pairs { get; set; } = [];
}
