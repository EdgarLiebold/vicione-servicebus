using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
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
    [InlineData(MessageBodyFirstAccessor.String)]
    [InlineData(MessageBodyFirstAccessor.Stream)]
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

    private sealed record BodyMessage(int Id, string Text);
}
