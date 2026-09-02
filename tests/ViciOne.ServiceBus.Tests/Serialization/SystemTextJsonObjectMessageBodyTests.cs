using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonObjectMessageBodyTests
{
    [Theory]
    [InlineData(MessageBodyFirstAccessor.Length)]
    [InlineData(MessageBodyFirstAccessor.Bytes)]
    [InlineData(MessageBodyFirstAccessor.String)]
    [InlineData(MessageBodyFirstAccessor.Stream)]
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

    private sealed record BodyMessage(int Id, string Text);
}
