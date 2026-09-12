using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonMessageBodyTests
{
    private static readonly Guid MessageId = Guid.Parse("82ea3b16-d50d-4ab8-9f7e-86bb26827f08");
    private static readonly DateTime SentTime = new(2026, 8, 23, 12, 34, 56, DateTimeKind.Utc);

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ENVELOPE-BODY", "wire-header-contract-filters-null-values")]
    public void WireHeaders_PreserveComparerAndLiveUpdatesWithoutExposingNullValues()
    {
        var envelope = new JsonMessageEnvelope
        {
            Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mixed-Case"] = "before",
                ["Null-Value"] = null,
            },
        };
        var context = new EnvelopeMessageContext(envelope, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.True(context.Headers.TryGetHeader("mixed-case", out object? before));
        Assert.Equal("before", before);
        Assert.False(context.Headers.TryGetHeader("null-value", out object? nullValue));
        Assert.Null(nullValue);

        envelope.Headers["Mixed-Case"] = "after";

        Assert.True(context.Headers.TryGetHeader("MIXED-CASE", out object? after));
        Assert.Equal("after", after);
        Assert.Single(context.Headers.GetAll());
        Assert.Single(context.Headers);
    }

    [Theory]
    [InlineData(MessageBodyFirstAccessor.Length)]
    [InlineData(MessageBodyFirstAccessor.Bytes)]
    [InlineData(MessageBodyFirstAccessor.TransportText)]
    [InlineData(MessageBodyFirstAccessor.ReadStream)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ENVELOPE-BODY", "accessor-order-and-read-only-stream")]
    public void EveryAccessorOrder_ExposesTheExactReadOnlyEnvelope(
        MessageBodyFirstAccessor firstAccessor)
    {
        var message = new BodyMessage(27, "Grüße");
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        var envelope = new JsonMessageEnvelope
        {
            MessageId = MessageId.ToString("D"),
            MessageTypes = [MessageUrn.ForTypeString<BodyMessage>()],
            Message = message,
            SentTime = SentTime,
            Headers = new Dictionary<string, object?>
            {
                ["source"] = "contract-test",
            },
        };
        byte[] expectedBytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);
        string expectedText = JsonSerializer.Serialize(envelope, options);
        var context = new MessageSendContext<BodyMessage>(message);
        var body = new SystemTextJsonMessageBody<BodyMessage>(context, options, envelope);

        MessageBodyContractAssertions.AssertExactReadOnlyBody(
            body,
            expectedBytes,
            expectedText,
            firstAccessor);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ENVELOPE-BODY", "eager-owned-snapshot-single-serialization")]
    public void Construction_DetachesTheEnvelopeFromLaterMessageAndMetadataMutation()
    {
        var message = new MutableBodyMessage { Id = 27, Text = "before" };
        var expectedOptions = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        var envelope = new JsonMessageEnvelope
        {
            MessageId = MessageId.ToString("D"),
            MessageTypes = [MessageUrn.ForTypeString<MutableBodyMessage>()],
            Message = message,
            Headers = new Dictionary<string, object?> { ["source"] = "before" },
        };
        byte[] expected = JsonSerializer.SerializeToUtf8Bytes(envelope, expectedOptions);
        var converter = new CountingMutableBodyMessageConverter();
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(converter);
        var context = new MessageSendContext<MutableBodyMessage>(message);
        var body = new SystemTextJsonMessageBody<MutableBodyMessage>(context, options, envelope);
        Assert.Equal(1, converter.WriteCount);

        message.Id = 99;
        message.Text = "after";
        envelope.Headers["source"] = "after";
        envelope.MessageId = Guid.NewGuid().ToString("D");

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
