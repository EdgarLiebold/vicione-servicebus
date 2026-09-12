using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonRawMessageBodyTests
{
    [Theory]
    [InlineData(MessageBodyFirstAccessor.Length)]
    [InlineData(MessageBodyFirstAccessor.Bytes)]
    [InlineData(MessageBodyFirstAccessor.TransportText)]
    [InlineData(MessageBodyFirstAccessor.ReadStream)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-RAW-BODY", "accessor-order-and-read-only-stream")]
    public void EveryAccessorOrder_ExposesTheExactReadOnlyRawMessage(
        MessageBodyFirstAccessor firstAccessor)
    {
        var message = new BodyMessage(27, "Grüße");
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        byte[] expectedBytes = JsonSerializer.SerializeToUtf8Bytes(message, options);
        string expectedText = JsonSerializer.Serialize(message, options);
        var context = new MessageSendContext<BodyMessage>(message);
        var body = new SystemTextJsonRawMessageBody<BodyMessage>(context, options, message);

        MessageBodyContractAssertions.AssertExactReadOnlyBody(
            body,
            expectedBytes,
            expectedText,
            firstAccessor);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-RAW-BODY", "eager-owned-snapshot-single-serialization")]
    public void Construction_DetachesTheRawBodyFromLaterMessageMutation()
    {
        var message = new MutableBodyMessage { Id = 27, Text = "before" };
        var expectedOptions = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        byte[] expected = JsonSerializer.SerializeToUtf8Bytes(message, expectedOptions);
        var converter = new CountingMutableBodyMessageConverter();
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(converter);
        var context = new MessageSendContext<MutableBodyMessage>(message);
        var body = new SystemTextJsonRawMessageBody<MutableBodyMessage>(context, options);
        Assert.Equal(1, converter.WriteCount);

        message.Id = 99;
        message.Text = "after";

        Parallel.For(0, 128, _ =>
        {
            Assert.Equal(expected, body.ToArray());
            Assert.Equal(expected, MessageBodyContractAssertions.Read(body.OpenReadStream()));
            Assert.Equal(System.Text.Encoding.UTF8.GetString(expected), body.GetRequiredTransportText());
        });

        Assert.Equal(1, converter.WriteCount);
    }

    private sealed record BodyMessage(int Id, string Text);

    private sealed class MutableBodyMessage
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    private sealed class CountingMutableBodyMessageConverter : JsonConverter<MutableBodyMessage>
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
