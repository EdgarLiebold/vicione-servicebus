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
            SystemTextJsonMessageSerializer.Options);

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

        CountedMessage? result = source.Transform<CountedMessage>(SystemTextJsonMessageSerializer.Options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    public sealed class CountedMessage
    {
        public string? Name { get; init; }

        public int Count { get; init; }
    }
}
