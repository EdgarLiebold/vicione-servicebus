using System.Text.Json;
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
    [InlineData(MessageBodyFirstAccessor.String)]
    [InlineData(MessageBodyFirstAccessor.Stream)]
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

    private sealed record BodyMessage(int Id, string Text);
}
