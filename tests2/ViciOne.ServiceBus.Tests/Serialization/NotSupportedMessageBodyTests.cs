using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class NotSupportedMessageBodyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NOT-SUPPORTED-MESSAGE-BODY", "every-member-rejects-access")]
    public void EveryMember_ThrowsNotSupportedException()
    {
        var body = new NotSupportedMessageBody();

        Assert.Throws<NotSupportedException>(() => body.Length);
        Assert.Throws<NotSupportedException>(() => body.GetBytes());
        Assert.Throws<NotSupportedException>(() => body.GetString());
        Assert.Throws<NotSupportedException>(() => body.GetStream());
    }
}
