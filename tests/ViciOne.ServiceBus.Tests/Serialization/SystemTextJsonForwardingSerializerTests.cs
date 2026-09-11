using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonForwardingSerializerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "recursive-object-and-array-overlay")]
    public void ObjectOverlay_RecursivelyMergesObjectsAndAppendsArrayElements()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new
        {
            values = new[] { 1 },
            nested = new { replaced = 2, preserved = 3 },
            preserved = "original",
        });
        var serializer = CreateRawSerializer(original);
        serializer.Overlay(new
        {
            values = new[] { 4, 5 },
            nested = new { replaced = 7 },
            preserved = (string?)null,
            added = true,
        });

        JsonElement result = Serialize(serializer);

        Assert.Equal([1, 4, 5], result.GetProperty("values").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(7, result.GetProperty("nested").GetProperty("replaced").GetInt32());
        Assert.Equal(3, result.GetProperty("nested").GetProperty("preserved").GetInt32());
        Assert.Equal("original", result.GetProperty("preserved").GetString());
        Assert.True(result.GetProperty("added").GetBoolean());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "top-level-array-overlay")]
    public void ArrayOverlay_AppendsReplacementElementsInOrder()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new[] { 1, 2 });
        var serializer = CreateRawSerializer(original);

        serializer.Overlay(new[] { 3, 4 });

        Assert.Equal([1, 2, 3, 4], Serialize(serializer).EnumerateArray().Select(x => x.GetInt32()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "incompatible-shape-replacement")]
    public void IncompatibleOverlayShape_ReplacesTheOriginalJsonValue()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new[] { 1, 2 });
        var serializer = CreateRawSerializer(original);

        serializer.Overlay(new { value = 73 });

        Assert.Equal(73, Serialize(serializer).GetProperty("value").GetInt32());
    }

    private static SystemTextJsonForwardingSerializer CreateRawSerializer(JsonElement message) =>
        new(message, SystemTextJsonRawMessageSerializer.JsonContentType, ServiceBusMetadataJson.Options, RawSerializerOptions.None);

    private static JsonElement Serialize(SystemTextJsonForwardingSerializer serializer)
    {
        var context = new MessageSendContext<object>(new object());
        return JsonSerializer.Deserialize<JsonElement>(serializer.GetMessageBody(context).GetBytes(), ServiceBusMetadataJson.Options);
    }
}
