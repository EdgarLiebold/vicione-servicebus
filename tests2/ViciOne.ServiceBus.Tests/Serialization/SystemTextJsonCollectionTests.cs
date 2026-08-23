using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "null-array")]
    public void NullArray_IsEncodedAndRestoredAsNull()
    {
        SystemTextJsonRoundTripResult<ArrayCollectionMessage> result =
            SystemTextJsonRoundTrip.ExecuteWithContext(new ArrayCollectionMessage());

        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        JsonElement elements = document.RootElement.GetProperty("message").GetProperty("array");

        Assert.Equal(JsonValueKind.Null, elements.ValueKind);
        Assert.Null(result.Message.Array);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "array-and-icollection")]
    public void SingleElementArrayAndCollection_RoundTripWithExactValues()
    {
        var expectedArray = new CollectionElement(27, "array");
        var expectedCollection = new CollectionElement(42, "collection");
        var source = new ArrayCollectionMessage
        {
            Array = [expectedArray],
            Collection = [expectedCollection],
        };

        ArrayCollectionMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(expectedArray, Assert.Single(result.Array!));
        Assert.Equal(expectedCollection, Assert.Single(result.Collection!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "interface-ienumerable")]
    public void InterfaceEnumerable_RoundTripsWithExactOrderedItems()
    {
        EnumerableMessage source = new EnumerableMessageImplementation
        {
            Items =
            [
                new CollectionElement(1, "Frank"),
                new CollectionElement(2, "Mary"),
            ],
        };

        EnumerableMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(
            [new CollectionElement(1, "Frank"), new CollectionElement(2, "Mary")],
            result.Items);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "duplicate-key-pairs")]
    public void DuplicateKeyPairs_RemainAnOrderedListWithoutKeyCollapse()
    {
        var source = new DuplicateKeyPairMessage
        {
            Properties =
            [
                new("Frank", "Mary"),
                new("Peter", "Mary"),
                new("Frank", "Peter"),
            ],
        };

        DuplicateKeyPairMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(["Frank", "Peter", "Frank"], result.Properties.Select(pair => pair.Key));
        Assert.Equal(["Mary", "Mary", "Peter"], result.Properties.Select(pair => ReadString(pair.Value)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "nested-object-list")]
    public void NestedObjectList_RoundTripsEveryValueInOrder()
    {
        var source = new NestedCollectionListMessage
        {
            Elements =
            [
                new(new CollectionElement(1, "first")),
                new(new CollectionElement(2, "second")),
            ],
        };

        NestedCollectionListMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Collection(
            result.Elements,
            element => Assert.Equal(new CollectionElement(1, "first"), element.Inner),
            element => Assert.Equal(new CollectionElement(2, "second"), element.Inner));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "dictionary-cardinality")]
    public void Dictionary_RoundTripsEmptySingleAndMultipleEntries(int count)
    {
        Dictionary<string, NestedCollectionValue> expected = Enumerable.Range(0, count)
            .ToDictionary(
                index => $"key-{index}",
                index => new NestedCollectionValue(new CollectionElement(index, $"value-{index}")),
                StringComparer.Ordinal);
        var source = new NestedCollectionDictionaryMessage
        {
            Elements = expected.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal),
        };

        NestedCollectionDictionaryMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(expected.Count, result.Elements.Count);
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), result.Elements.Keys.Order(StringComparer.Ordinal));

        foreach ((string key, NestedCollectionValue expectedValue) in expected)
        {
            NestedCollectionValue actualValue = Assert.Contains(key, result.Elements);
            Assert.Equal(expectedValue.Inner, actualValue.Inner);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "concrete-and-interface-sets")]
    public void ConcreteAndInterfaceSets_RoundTripExactMembership()
    {
        var source = new SetCollectionMessage
        {
            ConcreteValues = [8, 3, 5],
            InterfaceValues = new HashSet<int> { 13, 2, 7 },
        };

        SetCollectionMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal([3, 5, 8], result.ConcreteValues.Order());
        Assert.Equal([2, 7, 13], result.InterfaceValues.Order());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "nested-object")]
    public void NestedObject_RoundTripsItsExactValue()
    {
        var source = new NestedCollectionValue(new CollectionElement(27, "nested"));

        NestedCollectionValue result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(new CollectionElement(27, "nested"), result.Inner);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "primitive-array-cardinality")]
    public void PrimitiveArray_RoundTripsEmptySingleAndManyElements(int count)
    {
        int[] expected = Enumerable.Range(1, count).ToArray();
        var source = new PrimitiveArrayCollectionMessage { Values = [.. expected] };

        PrimitiveArrayCollectionMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(expected, result.Values);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "read-only-dictionary-lists")]
    public void ReadOnlyDictionaryOfLists_RoundTripsTheExactStructure()
    {
        var source = new ReadOnlyDictionaryCollectionMessage
        {
            Values = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["first"] = ["alpha", "beta"],
                ["empty"] = [],
            },
        };

        ReadOnlyDictionaryCollectionMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(["empty", "first"], result.Values.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["alpha", "beta"], result.Values["first"]);
        Assert.Empty(result.Values["empty"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "generic-object-array")]
    public void GenericObjectArray_RoundTripsEveryValueInOrder()
    {
        var source = new GenericArrayCollectionMessage<CollectionElement>
        {
            Values = [new(1, "first"), new(2, "second")],
        };

        GenericArrayCollectionMessage<CollectionElement> result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal([new CollectionElement(1, "first"), new CollectionElement(2, "second")], result.Values);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "empty-contract")]
    public void EmptyContract_RoundTripsAsANewInstanceOfTheSameType()
    {
        var source = new EmptyCollectionContract();

        EmptyCollectionContract result = SystemTextJsonRoundTrip.Execute(source);

        Assert.IsType<EmptyCollectionContract>(result);
        Assert.NotSame(source, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-COLLECTIONS", "enum-value")]
    public void EnumValue_RoundTripsExactly()
    {
        var source = new EnumCollectionMessage { State = CollectionSampleState.Second };

        EnumCollectionMessage result = SystemTextJsonRoundTrip.Execute(source);

        Assert.Equal(CollectionSampleState.Second, result.State);
    }

    private static string ReadString(object value)
    {
        JsonElement element = Assert.IsType<JsonElement>(value);
        return element.GetString() ?? throw new InvalidDataException("The pair value was not a JSON string.");
    }
}

public sealed record CollectionElement(int Sequence, string Value);

public sealed class ArrayCollectionMessage
{
    public CollectionElement[]? Array { get; init; }

    public ICollection<CollectionElement>? Collection { get; init; }
}

public interface EnumerableMessage
{
    IEnumerable<CollectionElement> Items { get; }
}

public sealed class EnumerableMessageImplementation :
    EnumerableMessage
{
    public IEnumerable<CollectionElement> Items { get; init; } = [];
}

public sealed class DuplicateKeyPairMessage
{
    public List<KeyValuePair<string, object>> Properties { get; init; } = [];
}

public sealed record NestedCollectionValue(CollectionElement Inner);

public sealed class NestedCollectionListMessage
{
    public IList<NestedCollectionValue> Elements { get; init; } = [];
}

public sealed class NestedCollectionDictionaryMessage
{
    public IDictionary<string, NestedCollectionValue> Elements { get; init; } =
        new Dictionary<string, NestedCollectionValue>(StringComparer.Ordinal);
}

public sealed class SetCollectionMessage
{
    public HashSet<int> ConcreteValues { get; init; } = [];

    public ISet<int> InterfaceValues { get; init; } = new HashSet<int>();
}

public sealed class PrimitiveArrayCollectionMessage
{
    public int[] Values { get; init; } = [];
}

public sealed class ReadOnlyDictionaryCollectionMessage
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Values { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
}

public sealed class GenericArrayCollectionMessage<T>
{
    public T[] Values { get; init; } = [];
}

public sealed class EmptyCollectionContract;

public sealed class EnumCollectionMessage
{
    public CollectionSampleState State { get; init; }
}

public enum CollectionSampleState
{
    First,
    Second,
    Third,
}
