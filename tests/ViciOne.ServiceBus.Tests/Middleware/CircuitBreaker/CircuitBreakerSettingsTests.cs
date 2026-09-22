using ViciOne.ServiceBus.Middleware.CircuitBreaker;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker;

public sealed class CircuitBreakerSettingsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-complete-invalid-aggregation")]
    public void Validate_ReportsEveryIndependentInvalidSetting()
    {
        var settings = new CircuitBreakerSettings(
            -1,
            double.NaN,
            TimeSpan.FromTicks(-1),
            [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)],
            null!,
            null!);

        ValidationResult[] failures = settings.Validate().ToArray();

        Assert.Equal(
            [
                nameof(CircuitBreakerSettings.MinimumThroughput),
                nameof(CircuitBreakerSettings.FailureRatio),
                nameof(CircuitBreakerSettings.SamplingDuration),
                nameof(CircuitBreakerSettings.BreakDurations),
                nameof(CircuitBreakerSettings.TimeProvider),
                nameof(CircuitBreakerSettings.ExceptionFilter),
            ],
            failures.Select(failure => failure.Key).ToArray());
        Assert.All(failures, failure => Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-inclusive-limits")]
    public void Validate_AcceptsInclusiveRatioLimitsAndEqualDurations()
    {
        CircuitBreakerSettings lowerLimit = ValidSettings(0);
        CircuitBreakerSettings upperLimit = ValidSettings(1) with
        {
            BreakDurations = [TimeSpan.FromTicks(1), TimeSpan.FromTicks(1)],
        };

        Assert.Empty(lowerLimit.Validate());
        Assert.Empty(upperLimit.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-minimum-throughput-lower-bound")]
    public void Validate_RejectsEveryMinimumThroughputBelowOne(int minimumThroughput)
    {
        CircuitBreakerSettings settings = ValidSettings(0) with { MinimumThroughput = minimumThroughput };

        AssertFailure(
            settings,
            nameof(CircuitBreakerSettings.MinimumThroughput),
            "must be at least one");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-sampling-duration-lower-bound")]
    public void Validate_RejectsEveryNonpositiveSamplingDuration(long ticks)
    {
        CircuitBreakerSettings settings = ValidSettings(0) with { SamplingDuration = TimeSpan.FromTicks(ticks) };

        AssertFailure(
            settings,
            nameof(CircuitBreakerSettings.SamplingDuration),
            "must be greater than zero");
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-nonfinite-and-out-of-range-ratios")]
    public void Validate_RejectsEveryNonfiniteOrOutOfRangeFailureRatio(double ratio)
    {
        CircuitBreakerSettings settings = ValidSettings(ratio);

        AssertFailure(
            settings,
            nameof(CircuitBreakerSettings.FailureRatio),
            "must be between 0.0 and 1.0");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-break-duration-presence")]
    public void Validate_RejectsNullAndEmptyBreakDurationSequences()
    {
        AssertFailure(
            ValidSettings(0) with { BreakDurations = null! },
            nameof(CircuitBreakerSettings.BreakDurations),
            "must contain at least one duration");
        AssertFailure(
            ValidSettings(0) with { BreakDurations = [] },
            nameof(CircuitBreakerSettings.BreakDurations),
            "must contain at least one duration");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-break-duration-positivity")]
    public void Validate_RejectsEveryNonpositiveBreakDuration(long ticks)
    {
        CircuitBreakerSettings settings = ValidSettings(0) with
        {
            BreakDurations = [TimeSpan.FromTicks(1), TimeSpan.FromTicks(ticks)],
        };

        AssertFailure(
            settings,
            nameof(CircuitBreakerSettings.BreakDurations),
            "must contain only positive durations");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "runtime-settings-break-duration-order")]
    public void Validate_RejectsDescendingBreakDurations()
    {
        CircuitBreakerSettings settings = ValidSettings(0) with
        {
            BreakDurations =
            [
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
            ],
        };

        AssertFailure(
            settings,
            nameof(CircuitBreakerSettings.BreakDurations),
            "must be ordered from shortest to longest");
    }

    private static CircuitBreakerSettings ValidSettings(double failureRatio) => new(
        1,
        failureRatio,
        TimeSpan.FromTicks(1),
        [TimeSpan.FromTicks(1)],
        TimeProvider.System,
        Retry.All());

    private static void AssertFailure(CircuitBreakerSettings settings, string key, string message)
    {
        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal(key, failure.Key);
        Assert.Equal(message, failure.Message);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
    }
}
