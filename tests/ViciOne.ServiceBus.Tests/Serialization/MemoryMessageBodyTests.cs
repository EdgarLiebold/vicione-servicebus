using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class MemoryMessageBodyTests
{
    private const string ExpectedText = "aäあb";

    [Theory]
    [InlineData(MessageBodyFirstAccessor.Length)]
    [InlineData(MessageBodyFirstAccessor.Bytes)]
    [InlineData(MessageBodyFirstAccessor.String)]
    [InlineData(MessageBodyFirstAccessor.Stream)]
    [RequirementCoverage("REQ-VSB-MEMORY-MESSAGE-BODY", "accessor-order-and-read-only-stream")]
    public void EveryAccessorOrder_ExposesOnlyTheSelectedReadOnlyMemory(
        MessageBodyFirstAccessor firstAccessor)
    {
        byte[] expected = MessageBodyContractAssertions.Utf8(ExpectedText);
        byte[] backing = [0xFE, .. expected, 0xFD];
        var body = new MemoryMessageBody(backing.AsMemory(1, expected.Length));

        MessageBodyContractAssertions.AssertExactReadOnlyBody(
            body,
            expected,
            ExpectedText,
            firstAccessor);
    }
}
