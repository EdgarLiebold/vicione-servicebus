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
