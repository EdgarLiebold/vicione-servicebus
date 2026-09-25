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

    [Theory]
    [InlineData(1, 1, 1, 1, 1)]
    [InlineData(1024, 1024, 32, 1024, 1024)]
    [InlineData(1024, 2048, 32, null, null)]
    [InlineData(1024, 2048, 32, 512, null)]
    [InlineData(1024, 2048, 32, null, 512)]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "valid-boundaries")]
    public void Validate_AcceptsInclusiveSizeLimitsAndOptionalThresholds(
        int body, int envelope, int depth, int? warning, int? offload)
    {
        var limits = CreateLimits(body, envelope, depth, warning, offload);

        MessageLimits validated = limits.Validate("orders");

        Assert.Same(limits, validated);
        Assert.Equal(body, validated.MaxBodyBytes);
        Assert.Equal(envelope, validated.MaxEnvelopeBytes);
        Assert.Equal(depth, validated.MaxJsonDepth);
        Assert.Equal(warning, validated.WarnAboveBytes);
        Assert.Equal(offload, validated.OffloadToMessageDataAboveBytes);
    }

    [Theory]
    [InlineData(0, 20, 1, null, null, "MaxBodyBytes", "greater than zero")]
    [InlineData(-1, 20, 1, null, null, "MaxBodyBytes", "greater than zero")]
    [InlineData(10, 0, 1, null, null, "MaxEnvelopeBytes", "greater than zero")]
    [InlineData(10, -1, 1, null, null, "MaxEnvelopeBytes", "greater than zero")]
    [InlineData(10, 9, 1, null, null, "MaxEnvelopeBytes", "must not be less than")]
    [InlineData(10, 20, 0, null, null, "MaxJsonDepth", "greater than zero")]
    [InlineData(10, 20, -1, null, null, "MaxJsonDepth", "greater than zero")]
    [InlineData(10, 20, 1, 0, null, "WarnAboveBytes", "greater than zero")]
    [InlineData(10, 20, 1, -1, null, "WarnAboveBytes", "greater than zero")]
    [InlineData(10, 20, 1, 11, null, "WarnAboveBytes", "must not exceed")]
    [InlineData(10, 20, 1, null, 0, "OffloadToMessageDataAboveBytes", "greater than zero")]
    [InlineData(10, 20, 1, null, -1, "OffloadToMessageDataAboveBytes", "greater than zero")]
    [InlineData(10, 20, 1, null, 11, "OffloadToMessageDataAboveBytes", "must not exceed")]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "invalid-limits-diagnostics")]
    public void Validate_RejectsInvalidLimitWithBusAndFieldInDiagnostic(
        int body, int envelope, int depth, int? warning, int? offload, string property, string reason)
    {
        var limits = CreateLimits(body, envelope, depth, warning, offload);

        var error = Assert.Throws<ConfigurationException>(() => limits.Validate("orders"));

        Assert.Contains("bus 'orders'", error.Message, StringComparison.Ordinal);
        Assert.Contains(property, error.Message, StringComparison.Ordinal);
        Assert.Contains(reason, error.Message, StringComparison.Ordinal);
    }

    private static MessageLimits CreateLimits(int body, int envelope, int depth, int? warning, int? offload)
        => new()
        {
            MaxBodyBytes = body,
            MaxEnvelopeBytes = envelope,
            MaxJsonDepth = depth,
            WarnAboveBytes = warning,
            OffloadToMessageDataAboveBytes = offload,
        };
}
