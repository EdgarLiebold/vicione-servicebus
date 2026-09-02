using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonInterfaceMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-INTERFACE-METADATA", "property-converter")]
    public void InterfacePropertyConverter_IsHonoredByTheGeneratedImplementation()
    {
        var serializer = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);

        ConverterAttributedMessage? result = serializer.DeserializeObject<ConverterAttributedMessage>("{\"value\":10}");

        Assert.NotNull(result);
        Assert.Equal(-1, result.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-INTERFACE-METADATA", "nullable-empty-sequence")]
    public void NullableInterfaceCollection_DeserializesEmptyArrayAsANonNullEmptySequence()
    {
        var serializer = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);

        NullableCollectionMessage? result = serializer.DeserializeObject<NullableCollectionMessage>("{\"bars\":[]}");

        Assert.NotNull(result);
        Assert.NotNull(result.Bars);
        Assert.Empty(result.Bars);
    }
}

public sealed class NegativeOneIntConverter :
    JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out _))
            throw new JsonException("The fixture converter requires an integer JSON token.");

        return -1;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}

public interface ConverterAttributedMessage
{
    [JsonConverter(typeof(NegativeOneIntConverter))]
    int Value { get; }
}

public interface NullableCollectionMessage
{
    IEnumerable<InterfaceCollectionElement>? Bars { get; }
}

public interface InterfaceCollectionElement;
