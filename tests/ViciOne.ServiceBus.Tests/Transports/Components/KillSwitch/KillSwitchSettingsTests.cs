using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Components;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Components.KillSwitch;

public sealed class KillSwitchSettingsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "complete-validation-boundaries")]
    public void Validate_ReportsEveryInvalidSettingAndAcceptsTheInclusiveLimits()
    {
        var invalid = new KillSwitchSettings(
            0,
            double.NaN,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(999),
            null!,
            null!);

        ValidationResult[] failures = invalid.Validate().ToArray();

        Assert.Equal(
            [
                nameof(KillSwitchSettings.ActivationThreshold),
                nameof(KillSwitchSettings.TripThresholdRatio),
                nameof(KillSwitchSettings.TrackingPeriod),
                nameof(KillSwitchSettings.RestartDelay),
                nameof(KillSwitchSettings.TimeProvider),
                nameof(KillSwitchSettings.ExceptionFilter),
            ],
            failures.Select(failure => failure.Key).ToArray());
        Assert.All(failures, failure => Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition));

        var lowerLimit = new KillSwitchSettings(
            1,
            0,
            TimeSpan.FromTicks(1),
            TimeSpan.FromSeconds(1),
            TimeProvider.System,
            Retry.All());
        var upperLimit = lowerLimit with { TripThresholdRatio = 1 };

        Assert.Empty(lowerLimit.Validate());
        Assert.Empty(upperLimit.Validate());
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "nonfinite-and-out-of-range-ratios")]
    public void Validate_RejectsEveryNonfiniteOrOutOfRangeTripRatio(double ratio)
    {
        var settings = new KillSwitchSettings(
            1,
            ratio,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeProvider.System,
            Retry.All());

        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal(nameof(KillSwitchSettings.TripThresholdRatio), failure.Key);
        Assert.Contains("between 0.0 and 1.0", failure.Message, StringComparison.Ordinal);
    }
}
