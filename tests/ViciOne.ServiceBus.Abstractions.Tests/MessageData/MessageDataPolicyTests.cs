using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.MessageData;

public sealed class MessageDataPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "immutable-defaults")]
    public void DefaultPolicy_UsesTheDocumentedImmutableValues()
    {
        MessageDataPolicy policy = MessageDataPolicy.Default;

        Assert.True(policy.AlwaysWriteToRepository);
        Assert.Equal(4096, policy.Threshold);
        Assert.Null(policy.TimeToLive);
        Assert.Null(policy.ExtraTimeToLive);
        Assert.Same(policy, MessageDataPolicy.Default);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "constructor-validation")]
    public void Constructor_RejectsEveryInvalidBoundary()
    {
        Assert.Equal("threshold", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDataPolicy(threshold: -1)).ParamName);
        Assert.Equal("timeToLive", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDataPolicy(timeToLive: TimeSpan.Zero)).ParamName);
        Assert.Equal("timeToLive", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDataPolicy(timeToLive: TimeSpan.FromTicks(-1))).ParamName);
        Assert.Equal("extraTimeToLive", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDataPolicy(extraTimeToLive: TimeSpan.Zero)).ParamName);
        Assert.Equal("extraTimeToLive", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDataPolicy(extraTimeToLive: TimeSpan.FromTicks(-1))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "zero-threshold-is-valid")]
    public void ZeroThreshold_IsAnExplicitAlwaysStoredBoundary()
    {
        var policy = new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 0);

        Assert.False(policy.AlwaysWriteToRepository);
        Assert.Equal(0, policy.Threshold);
    }
}
