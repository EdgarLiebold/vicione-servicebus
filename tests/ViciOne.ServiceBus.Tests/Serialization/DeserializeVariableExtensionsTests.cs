using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class DeserializeVariableExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZE-VARIABLE", "reference-and-value-conversion")]
    public void TryGetValue_ConvertsReferenceAndValueEntriesWithTheStableMetadataCodec()
    {
        IDictionary<string, object?> values = new Dictionary<string, object?>
        {
            ["reference"] = ServiceBusMetadataSerializer.Serialize(new MetadataValue("north")),
            ["value"] = ServiceBusMetadataSerializer.Serialize(27),
        };

        bool foundReference = values.TryGetValue<MetadataValue>("reference", out MetadataValue? reference);
        bool foundValue = values.TryGetValue<int>("value", out int? value);

        Assert.True(foundReference);
        Assert.Equal(new MetadataValue("north"), reference);
        Assert.True(foundValue);
        Assert.Equal(27, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZE-VARIABLE", "absence-and-key-validation")]
    public void TryGetValue_ReportsAbsenceAndRejectsInvalidKeysForBothTypeKinds()
    {
        IDictionary<string, object?>? values = null;

        Assert.False(values.TryGetValue<MetadataValue>("missing", out MetadataValue? reference));
        Assert.Null(reference);
        Assert.False(values.TryGetValue<int>("missing", out int? number));
        Assert.Null(number);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            values.TryGetValue<MetadataValue>(" ", out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            values.TryGetValue<int>("", out _)).ParamName);
    }

    private sealed record MetadataValue(string Region);
}
