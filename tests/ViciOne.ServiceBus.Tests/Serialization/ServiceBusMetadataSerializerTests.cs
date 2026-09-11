using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class ServiceBusMetadataSerializerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-SERIALIZER", "serialize-null-and-value")]
    public void Serialize_ReturnsNullForNullAndStableJsonForAValue()
    {
        Assert.Null(ServiceBusMetadataSerializer.Serialize(null));

        string? json = ServiceBusMetadataSerializer.Serialize(new MetadataValue("north", 27));

        Assert.NotNull(json);
        MetadataValue? restored = JsonSerializer.Deserialize<MetadataValue>(json, ServiceBusMetadataJson.Options);
        Assert.Equal(new MetadataValue("north", 27), restored);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-SERIALIZER", "reference-input-and-defaults")]
    public void DeserializeReferenceType_ConvertsNativeAndSerializedValuesAndHonorsAbsenceDefaults()
    {
        var native = new MetadataValue("north", 27);
        var fallback = new MetadataValue("fallback", 0);
        string? serialized = ServiceBusMetadataSerializer.Serialize(native);

        Assert.Same(native, ServiceBusMetadataSerializer.DeserializeReference(native, fallback));
        Assert.Equal(native, ServiceBusMetadataSerializer.DeserializeReference(serialized, fallback));
        Assert.Same(fallback, ServiceBusMetadataSerializer.DeserializeReference<MetadataValue>(null, fallback));
        Assert.Same(fallback, ServiceBusMetadataSerializer.DeserializeReference("  ", fallback));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-SERIALIZER", "value-input-and-defaults")]
    public void DeserializeValueType_ConvertsNativeAndSerializedValuesAndHonorsAbsenceDefaults()
    {
        string? serialized = ServiceBusMetadataSerializer.Serialize(27);

        Assert.Equal(27, ServiceBusMetadataSerializer.DeserializeValue<int>(serialized));
        Assert.Null(ServiceBusMetadataSerializer.DeserializeValue<int>(null));
        Assert.Equal(27, ServiceBusMetadataSerializer.DeserializeValue(27, -1));
        Assert.Equal(27, ServiceBusMetadataSerializer.DeserializeValue(serialized, -1));
        Assert.Equal(-1, ServiceBusMetadataSerializer.DeserializeValue<int>(null, -1));
        Assert.Equal(-1, ServiceBusMetadataSerializer.DeserializeValue<int>("", -1));
    }

    public sealed record MetadataValue(string Region, int Priority);
}
