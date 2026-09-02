namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public interface BinaryContract
{
    byte[] Contents { get; set; }
}

public interface PersonContract
{
    int Id { get; init; }

    string Name { get; set; }

    ContactContract Contact { get; }
}

public interface ContactContract
{
    string Email { get; }
}

public sealed class PersonMessage : PersonContract
{
    public int Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public ContactContract Contact { get; set; } = new ContactMessage();
}

public sealed class ContactMessage : ContactContract
{
    public string Email { get; set; } = string.Empty;
}

public sealed class BinaryMessage
{
    public byte[] Contents { get; set; } = [];
}

public sealed class TemporalMessage
{
    public DateTime Local { get; set; }

    public DateTime Universal { get; set; }
}

public sealed class ConstructorBoundMessage
{
    public ConstructorBoundMessage(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; private set; }

    public string Value { get; private set; }
}

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

public sealed class NestedMessage
{
    public string Name { get; set; } = string.Empty;
}

public sealed class EdgeShapeMessage
{
    public EdgeShapeMessage(string privateValue)
    {
        PrivateValue = privateValue;
    }

    public string PrivateValue { get; private set; }

    public char Character { get; set; }

    public char? OptionalCharacter { get; set; }

    public ExampleState State { get; set; }
}

public enum ExampleState
{
    Unknown,
    Ready,
}

public sealed class EmptyMessage;

public sealed class ObjectGraphMessage
{
    public NestedMessage Nested { get; set; } = new();

    public List<NestedMessage> List { get; set; } = [];

    public NestedMessage[] Array { get; set; } = [];

    public KeyValuePair<string, string>[] Pairs { get; set; } = [];
}

public interface ValidationContract
{
    bool IsValid { get; }

    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }
}

public sealed class ValidationMessage : ValidationContract
{
    public bool IsValid { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>();
}

public sealed class XmlPayloadMessage
{
    public string Text { get; set; } = string.Empty;

    public byte[] Bytes { get; set; } = [];
}
