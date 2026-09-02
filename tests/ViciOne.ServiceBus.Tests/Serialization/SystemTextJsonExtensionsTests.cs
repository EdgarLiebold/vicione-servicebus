using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "json-element")]
    public void JsonElement_GetObjectCreatesTheExactRequestedShape()
    {
        using JsonDocument document = JsonDocument.Parse("{\"name\":\"bob\",\"count\":3}");

        CountedMessage? result = document.RootElement.GetObject<CountedMessage>(
            ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "dictionary-transform")]
    public void Dictionary_TransformCreatesTheExactRequestedShape()
    {
        var source = new Dictionary<string, object>
        {
            ["name"] = "bob",
            ["count"] = 3,
        };

        CountedMessage? result = source.Transform<CountedMessage>(ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "interface-with-caller-options")]
    public void JsonElement_GetObjectMaterializesAPublicInterfaceWithCallerOwnedOptions()
    {
        using JsonDocument document = JsonDocument.Parse("{\"name\":\"bob\",\"count\":3}");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        InterfaceValue? result = document.RootElement.GetObject<InterfaceValue>(options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    public sealed class CountedMessage
    {
        public string? Name { get; init; }

        public int Count { get; init; }
    }

    public interface InterfaceValue
    {
        string? Name { get; }

        int Count { get; }
    }
}
