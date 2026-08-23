using System.Text;
using System.Text.Json;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Serialization.Protobuf;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonApplicationFormatCompatibilityTests
{
    private const string EnvelopeJsonMediaType = "application/vnd.vicione.servicebus+json";
    private const string RawJsonMediaType = "application/json";
    private const string XmlDocument =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<order id=\"4711\"><customer name=\"Grüße &amp; Co\" />" +
        "<line sku=\"A-1\" qty=\"2\" />" +
        "<note><![CDATA[keep <this> verbatim]]></note></order>";

    private static readonly DateTime ObservedAt =
        new(2026, 8, 23, 10, 15, 30, DateTimeKind.Utc);

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PROTOBUF", "envelope-generated-contract")]
    public void EnvelopeJson_GeneratedProtobufContractRoundTripsEveryShape()
    {
        ProtobufCompatibilityPayload source = CreateProtobufPayload();

        SystemTextJsonRoundTripResult<ProtobufCompatibilityPayload> result =
            SystemTextJsonRoundTrip.ExecuteWithContext(source);

        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        Assert.True(document.RootElement.TryGetProperty("message", out JsonElement message));
        Assert.Equal(JsonValueKind.Object, message.ValueKind);
        AssertProtobufJson(source, message);
        Assert.Equal(EnvelopeJsonMediaType, result.ContentType);
        AssertProtobufPayload(source, result.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PROTOBUF", "raw-generated-contract")]
    public void RawJson_GeneratedProtobufContractRoundTripsEveryShape()
    {
        ProtobufCompatibilityPayload source = CreateProtobufPayload();

        SystemTextJsonRawRoundTripResult<ProtobufCompatibilityPayload> result =
            SystemTextJsonRoundTrip.ExecuteRawWithContext(source);

        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        AssertProtobufJson(source, document.RootElement);
        Assert.Equal(RawJsonMediaType, result.ContentType);
        AssertProtobufPayload(source, result.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-XML-PAYLOAD", "envelope-opaque-text-and-bytes")]
    public void EnvelopeJson_XmlRemainsOpaqueTextAndBytes()
    {
        XmlPayload source = CreateXmlPayload();

        SystemTextJsonRoundTripResult<XmlPayload> result =
            SystemTextJsonRoundTrip.ExecuteWithContext(source);

        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        Assert.True(document.RootElement.TryGetProperty("message", out JsonElement message));
        AssertXmlJson(source, message);
        AssertXmlPayload(source, result.Message);
        Assert.Equal(EnvelopeJsonMediaType, result.ContentType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-XML-PAYLOAD", "raw-opaque-text-and-bytes")]
    public void RawJson_XmlRemainsOpaqueTextAndBytes()
    {
        XmlPayload source = CreateXmlPayload();

        SystemTextJsonRawRoundTripResult<XmlPayload> result =
            SystemTextJsonRoundTrip.ExecuteRawWithContext(source);

        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        AssertXmlJson(source, document.RootElement);
        AssertXmlPayload(source, result.Message);
        Assert.Equal(RawJsonMediaType, result.ContentType);
    }

    private static ProtobufCompatibilityPayload CreateProtobufPayload()
    {
        var payload = new ProtobufCompatibilityPayload
        {
            Identifier = "protobuf-json-4711",
            ObservedAt = Timestamp.FromDateTime(ObservedAt),
        };
        payload.Values.Add(["AUD", "USD"]);

        return payload;
    }

    private static XmlPayload CreateXmlPayload() =>
        new(XmlDocument, Encoding.UTF8.GetBytes(XmlDocument));

    private static void AssertProtobufPayload(
        ProtobufCompatibilityPayload expected,
        ProtobufCompatibilityPayload actual)
    {
        Assert.NotSame(expected, actual);
        Assert.Equal(expected.Identifier, actual.Identifier);
        Assert.Equal(expected.Values, actual.Values);
        Assert.NotSame(expected.Values, actual.Values);
        Assert.Equal(ObservedAt, actual.ObservedAt.ToDateTime());
    }

    private static void AssertProtobufJson(ProtobufCompatibilityPayload expected, JsonElement payload)
    {
        Assert.Equal(["identifier", "values", "observedAt"], payload.EnumerateObject().Select(property => property.Name));
        Assert.Equal(expected.Identifier, payload.GetProperty("identifier").GetString());
        Assert.Equal(
            expected.Values,
            payload.GetProperty("values").EnumerateArray().Select(value => value.GetString()));

        JsonElement observedAt = payload.GetProperty("observedAt");
        Assert.Equal(["seconds", "nanos"], observedAt.EnumerateObject().Select(property => property.Name));
        Assert.Equal(expected.ObservedAt.Seconds, observedAt.GetProperty("seconds").GetInt64());
        Assert.Equal(expected.ObservedAt.Nanos, observedAt.GetProperty("nanos").GetInt32());
    }

    private static void AssertXmlJson(XmlPayload expected, JsonElement payload)
    {
        Assert.Equal(["text", "bytes"], payload.EnumerateObject().Select(property => property.Name));
        Assert.Equal(expected.Text, payload.GetProperty("text").GetString());
        Assert.Equal(Convert.ToBase64String(expected.Bytes), payload.GetProperty("bytes").GetString());
    }

    private static void AssertXmlPayload(XmlPayload expected, XmlPayload actual)
    {
        Assert.NotSame(expected, actual);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.NotSame(expected.Bytes, actual.Bytes);
        Assert.Equal(XmlDocument, Encoding.UTF8.GetString(actual.Bytes));
    }

    public sealed record XmlPayload(string Text, byte[] Bytes);
}
