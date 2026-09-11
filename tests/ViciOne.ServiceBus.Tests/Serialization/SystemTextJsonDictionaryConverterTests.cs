using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Serialization.Json.Converters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonDictionaryConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "default-scalars-preserved")]
    public void DefaultScalarValues_AreWrittenWhenDefaultSuppressionIsDisabled()
    {
        var values = new Dictionary<string, object>
        {
            ["dateTime"] = default(DateTime),
            ["dateTimeOffset"] = default(DateTimeOffset),
            ["guid"] = Guid.Empty,
        };

        JsonElement document = JsonSerializer.SerializeToElement<IDictionary<string, object>>(
            values,
            ServiceBusMetadataJson.Options);

        Assert.Equal(DateTime.MinValue, document.GetProperty("dateTime").GetDateTime());
        Assert.Equal(DateTimeOffset.MinValue, document.GetProperty("dateTimeOffset").GetDateTimeOffset());
        Assert.Equal(Guid.Empty, document.GetProperty("guid").GetGuid());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "all-default-value-types-suppressed")]
    public void DefaultScalarValues_AreOmittedConsistentlyWhenDefaultSuppressionIsEnabled()
    {
        JsonSerializerOptions options = SystemTextJsonSerializerOptions.CreateDefault();
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        var values = new Dictionary<string, object>
        {
            ["dateTime"] = default(DateTime),
            ["guid"] = Guid.Empty,
            ["float"] = 0F,
            ["unsigned"] = 0U,
            ["visible"] = 27,
        };

        JsonElement document = JsonSerializer.SerializeToElement<IDictionary<string, object>>(values, options);

        JsonProperty property = Assert.Single(document.EnumerateObject());
        Assert.Equal("visible", property.Name);
        Assert.Equal(27, property.Value.GetInt32());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "invalid-key-rejected")]
    public void InvalidStringKey_IsRejectedInsteadOfProducingMalformedJson()
    {
        IEnumerable<KeyValuePair<string, object>> values =
        [
            new KeyValuePair<string, object>(null!, 27),
        ];

        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(values, ServiceBusMetadataJson.Options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "unsupported-concrete-type-not-claimed")]
    public void ConverterFactory_DoesNotClaimConcreteDictionaryTypesItCannotMaterialize()
    {
        var factory = new SystemTextJsonConverterFactory();

        Assert.False(factory.CanConvert(typeof(ReadOnlyDictionary<string, int>)));
        Assert.False(factory.CanConvert(typeof(SortedDictionary<Uri, int>)));
        Assert.False(factory.CanConvert(typeof(Dictionary<int, string>)));
        Assert.False(factory.CanConvert(typeof(IReadOnlyList<KeyValuePair<string, int>>)));
        Assert.Throws<ViciOneServiceBusException>(() =>
            factory.CreateConverter(typeof(ReadOnlyDictionary<string, int>), ServiceBusMetadataJson.Options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "case-insensitive-materialization")]
    public void SupportedDictionaryInterface_IsMaterializedWithCaseInsensitiveKeys()
    {
        IDictionary<string, int>? values = JsonSerializer.Deserialize<IDictionary<string, int>>(
            "{\"Trace-Id\":27}",
            ServiceBusMetadataJson.Options);

        Assert.NotNull(values);
        Assert.Equal(27, values["TRACE-ID"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "supported-scalar-wire-representations")]
    public void SupportedScalarAndNestedValues_AreWrittenWithStableWireRepresentations()
    {
        var dateTime = new DateTime(2026, 9, 11, 8, 30, 0, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(2026, 9, 11, 10, 30, 0, TimeSpan.FromHours(2));
        var identifier = Guid.Parse("7346de5e-44e0-4f19-8ac7-14db303f262d");
        var values = new Dictionary<string, object>
        {
            ["null"] = null!,
            ["text"] = "north",
            ["dateTime"] = dateTime,
            ["dateTimeOffset"] = dateTimeOffset,
            ["guid"] = identifier,
            ["long"] = 9_000_000_000L,
            ["int"] = 27,
            ["short"] = (short)12,
            ["byte"] = (byte)3,
            ["float"] = 1.25F,
            ["double"] = 2.5D,
            ["decimal"] = 19.75M,
            ["flag"] = false,
            ["unsigned"] = 42U,
            ["nested"] = new Dictionary<string, int> { ["value"] = 73 },
        };

        JsonElement document = JsonSerializer.SerializeToElement<IDictionary<string, object>>(
            values,
            ServiceBusMetadataJson.Options);

        Assert.Equal(JsonValueKind.Null, document.GetProperty("null").ValueKind);
        Assert.Equal("north", document.GetProperty("text").GetString());
        Assert.Equal(dateTime, document.GetProperty("dateTime").GetDateTime());
        Assert.Equal(dateTimeOffset, document.GetProperty("dateTimeOffset").GetDateTimeOffset());
        Assert.Equal(identifier, document.GetProperty("guid").GetGuid());
        Assert.Equal(9_000_000_000L, document.GetProperty("long").GetInt64());
        Assert.Equal(27, document.GetProperty("int").GetInt32());
        Assert.Equal(12, document.GetProperty("short").GetInt16());
        Assert.Equal(3, document.GetProperty("byte").GetByte());
        Assert.Equal(1.25F, document.GetProperty("float").GetSingle());
        Assert.Equal(2.5D, document.GetProperty("double").GetDouble());
        Assert.Equal("19.75", document.GetProperty("decimal").GetString());
        Assert.False(document.GetProperty("flag").GetBoolean());
        Assert.Equal(42U, document.GetProperty("unsigned").GetUInt32());
        Assert.Equal(73, document.GetProperty("nested").GetProperty("value").GetInt32());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "nested-values-materialized")]
    public void NestedJsonValues_AreMaterializedWithoutLosingTheirScalarKinds()
    {
        const string Json = """
            {
              "Text": "north",
              "False": false,
              "True": true,
              "Null": null,
              "Integer": 27,
              "Fraction": 2.5,
              "Object": { "Nested": 73 },
              "Array": [1, "two", true, null]
            }
            """;

        IDictionary<string, object>? values = JsonSerializer.Deserialize<IDictionary<string, object>>(
            Json,
            ServiceBusMetadataJson.Options);

        Assert.NotNull(values);
        Assert.Equal("north", values["text"]);
        Assert.False(Assert.IsType<bool>(values["false"]));
        Assert.True(Assert.IsType<bool>(values["true"]));
        Assert.Null(values["null"]);
        Assert.Equal(27L, values["integer"]);
        Assert.Equal(2.5D, values["fraction"]);
        var nested = Assert.IsType<Dictionary<string, object>>(values["object"]);
        Assert.Equal(73L, nested["NESTED"]);
        var array = Assert.IsType<List<object>>(values["array"]);
        Assert.Equal(1L, array[0]);
        Assert.Equal("two", array[1]);
        Assert.True(Assert.IsType<bool>(array[2]));
        Assert.Null(array[3]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "key-value-array-materialized")]
    public void KeyValueArray_IsMaterializedAsACaseInsensitiveDictionary()
    {
        const string Json = """[{"Key":"Trace-Id","Value":27}]""";

        IEnumerable<KeyValuePair<string, object>>? entries =
            JsonSerializer.Deserialize<IEnumerable<KeyValuePair<string, object>>>(Json, ServiceBusMetadataJson.Options);

        var values = Assert.IsAssignableFrom<IDictionary<string, object>>(entries);
        Assert.Equal(27L, values["TRACE-ID"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "null-suppression-honored")]
    public void NullValues_AreOmittedOnlyWhenTheConfiguredPolicyRequiresIt()
    {
        var values = new Dictionary<string, object> { ["missing"] = null!, ["present"] = "value" };
        JsonSerializerOptions options = SystemTextJsonSerializerOptions.CreateDefault();
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        JsonElement suppressed = JsonSerializer.SerializeToElement<IDictionary<string, object>>(values, options);
        JsonElement preserved = JsonSerializer.SerializeToElement<IDictionary<string, object>>(
            values,
            ServiceBusMetadataJson.Options);

        Assert.False(suppressed.TryGetProperty("missing", out _));
        Assert.Equal("value", suppressed.GetProperty("present").GetString());
        Assert.Equal(JsonValueKind.Null, preserved.GetProperty("missing").ValueKind);
    }

    [Theory]
    [InlineData("27")]
    [InlineData("[27]")]
    [InlineData("{\"value\":27")]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "malformed-input-rejected")]
    public void InvalidDictionaryJson_IsRejected(string json)
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IDictionary<string, object>>(json, ServiceBusMetadataJson.Options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "uri-keys-round-trip")]
    public void UriKeyedDictionary_RoundTripsAbsoluteAndRelativeKeys()
    {
        Uri absolute = new("loopback://localhost/orders");
        Uri relative = new("relative/path", UriKind.Relative);
        IReadOnlyDictionary<Uri, int> values = new Dictionary<Uri, int>
        {
            [absolute] = 27,
            [relative] = 73,
        };

        string json = JsonSerializer.Serialize(values, ServiceBusMetadataJson.Options);
        IReadOnlyDictionary<Uri, int>? restored =
            JsonSerializer.Deserialize<IReadOnlyDictionary<Uri, int>>(json, ServiceBusMetadataJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(27, restored[absolute]);
        Assert.Equal(73, restored[relative]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "uri-duplicate-last-value-wins")]
    public void DuplicateUriProperty_UsesTheLastValue()
    {
        IDictionary<Uri, int>? restored = JsonSerializer.Deserialize<IDictionary<Uri, int>>(
            "{\"loopback://localhost/orders\":27,\"loopback://localhost/orders\":73}",
            ServiceBusMetadataJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(73, Assert.Single(restored).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "string-duplicate-last-value-wins")]
    public void DuplicateStringProperty_UsesTheLastValueCaseInsensitively()
    {
        IDictionary<string, int>? restored = JsonSerializer.Deserialize<IDictionary<string, int>>(
            "{\"Trace-Id\":27,\"trace-id\":73}",
            ServiceBusMetadataJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(73, Assert.Single(restored).Value);
        Assert.Equal(73, restored["TRACE-ID"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "uri-invalid-input-rejected")]
    public void InvalidUriKeys_AreRejectedForReadingAndWriting()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IDictionary<Uri, int>>(
            "{\"http://[::1\":27}",
            ServiceBusMetadataJson.Options));

        IEnumerable<KeyValuePair<Uri, int>> values = [new KeyValuePair<Uri, int>(null!, 27)];
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(values, ServiceBusMetadataJson.Options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "factory-creates-supported-shapes")]
    public void ConverterFactory_CreatesConvertersForEverySupportedDictionaryShape()
    {
        var factory = new SystemTextJsonConverterFactory();

        Assert.IsType<CaseInsensitiveDictionaryStringObjectJsonConverter<IDictionary<string, object>>>(
            factory.CreateConverter(typeof(IDictionary<string, object>), ServiceBusMetadataJson.Options));
        Assert.IsType<CaseInsensitiveDictionaryJsonConverter<IEnumerable<KeyValuePair<string, int>>, int>>(
            factory.CreateConverter(typeof(IEnumerable<KeyValuePair<string, int>>), ServiceBusMetadataJson.Options));
        Assert.IsType<UriDictionarySystemTextJsonConverter<IDictionary<Uri, int>, int>>(
            factory.CreateConverter(typeof(IDictionary<Uri, int>), ServiceBusMetadataJson.Options));
    }
}
