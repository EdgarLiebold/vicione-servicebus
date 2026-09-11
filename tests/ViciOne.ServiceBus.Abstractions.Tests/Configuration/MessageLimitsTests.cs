using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class MessageLimitsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "conservative-policy-has-stable-exact-values")]
    public void ConservativePolicy_IsOneStableInstanceWithTheDocumentedLimits()
    {
        MessageLimits first = MessageLimits.Conservative;
        MessageLimits second = MessageLimits.Conservative;

        Assert.Same(first, second);
        Assert.Equal(1024 * 1024, first.MaxBodyBytes);
        Assert.Equal(2 * 1024 * 1024, first.MaxEnvelopeBytes);
        Assert.Equal(32, first.MaxJsonDepth);
        Assert.Null(first.WarnAboveBytes);
        Assert.Null(first.OffloadToMessageDataAboveBytes);
    }
}
