namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class CollectionMessage
{
    public int[] Array { get; set; } = [];

    public HashSet<int> ConcreteSet { get; set; } = [];

    public ISet<int> InterfaceSet { get; set; } = new HashSet<int>();

    public IEnumerable<NestedMessage> Enumerable { get; set; } = [];

    public List<KeyValuePair<string, object>> DuplicateKeys { get; set; } = [];

    public Dictionary<string, NestedMessage> Dictionary { get; set; } = [];

    public int[,] Matrix { get; set; } = new int[0, 0];
}
