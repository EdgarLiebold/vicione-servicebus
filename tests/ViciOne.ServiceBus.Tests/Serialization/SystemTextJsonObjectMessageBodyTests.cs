using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonObjectMessageBodyTests
{
    [Theory]
    [InlineData(MessageBodyFirstAccessor.Length)]
    [InlineData(MessageBodyFirstAccessor.Bytes)]
    [InlineData(MessageBodyFirstAccessor.TransportText)]
    [InlineData(MessageBodyFirstAccessor.ReadStream)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-BODY", "accessor-order-and-read-only-stream")]
    public void EveryAccessorOrder_ExposesTheExactReadOnlyJsonObject(
        MessageBodyFirstAccessor firstAccessor)
    {
        var value = new BodyMessage(27, "Grüße");
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        byte[] expectedBytes = JsonSerializer.SerializeToUtf8Bytes(value, options);
        string expectedText = JsonSerializer.Serialize(value, options);
        var body = new SystemTextJsonObjectMessageBody(value, options);

        MessageBodyContractAssertions.AssertExactReadOnlyBody(
            body,
            expectedBytes,
            expectedText,
            firstAccessor);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-BODY", "eager-owned-snapshot-single-serialization")]
    public void Construction_SerializesExactlyOnceAndDetachesEveryLaterReadFromTheSource()
    {
        var value = new MutableBodyMessage { Id = 27, Text = "before" };
        var converter = new CountingBodyMessageConverter();
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(converter);

        var body = new SystemTextJsonObjectMessageBody(value, options);
        Assert.Equal(1, converter.WriteCount);
        byte[] snapshot = body.ToArray();
        value.Id = 99;
        value.Text = "after";

        Parallel.For(0, 128, _ =>
        {
            Assert.Equal(snapshot, body.ToArray());
            Assert.Equal(snapshot, MessageBodyContractAssertions.Read(body.OpenReadStream()));
            Assert.Equal(System.Text.Encoding.UTF8.GetString(snapshot), body.GetRequiredTransportText());
        });

        Assert.Equal(1, converter.WriteCount);
        Assert.Contains("before", body.GetRequiredTransportText(), StringComparison.Ordinal);
        Assert.DoesNotContain("after", body.GetRequiredTransportText(), StringComparison.Ordinal);
    }

    private sealed record BodyMessage(int Id, string Text);

    private sealed class MutableBodyMessage
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    private sealed class CountingBodyMessageConverter : JsonConverter<MutableBodyMessage>
    {
        int _writeCount;

        public int WriteCount => Volatile.Read(ref _writeCount);

        public override MutableBodyMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, MutableBodyMessage value, JsonSerializerOptions options)
        {
            Interlocked.Increment(ref _writeCount);
            writer.WriteStartObject();
            writer.WriteNumber("id", value.Id);
            writer.WriteString("text", value.Text);
            writer.WriteEndObject();
        }
    }
}
