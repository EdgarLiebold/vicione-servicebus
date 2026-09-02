using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonDecimalTests
{
    private const string MaximumDecimalText = "79228162514264337593543950335";

    [Fact]
    [RequirementCoverage(
        "REQ-VSB-SYSTEM-TEXT-JSON-DECIMAL",
        "maximum-value-quoted-wire-contract")]
    public void MaximumValue_UsesTheExactQuotedCamelCaseWireContract()
    {
        string json = JsonSerializer.Serialize(
            new DecimalMessage { Decimal = decimal.MaxValue },
            ServiceBusMetadataJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonProperty property = Assert.Single(document.RootElement.EnumerateObject());
        DecimalMessage? restored = JsonSerializer.Deserialize<DecimalMessage>(
            $$"""{"decimal":"{{MaximumDecimalText}}"}""",
            ServiceBusMetadataJson.Options);

        Assert.Equal("decimal", property.Name);
        Assert.Equal(JsonValueKind.String, property.Value.ValueKind);
        Assert.Equal(MaximumDecimalText, property.Value.GetString());
        Assert.NotNull(restored);
        Assert.Equal(decimal.MaxValue, restored.Decimal);
    }

    private sealed class DecimalMessage
    {
        public decimal Decimal { get; set; }
    }
}
