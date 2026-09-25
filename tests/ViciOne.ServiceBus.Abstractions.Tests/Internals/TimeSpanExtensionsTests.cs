using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals;

public sealed class TimeSpanExtensionsTests
{
    [Theory]
    [InlineData(0L, "-0-")]
    [InlineData(1L, "100ns")]
    [InlineData(10L, "1000ns")]
    [InlineData(11L, "1µs")]
    [InlineData(15L, "2µs")]
    [InlineData(10_000L, "1ms")]
    [InlineData(10_000_000L, "1s")]
    [InlineData(600_000_000L, "1m")]
    [InlineData(36_000_000_000L, "1h")]
    [InlineData(864_000_000_000L, "1d")]
    [InlineData(25_920_000_000_000L, "1M")]
    [InlineData(315_360_000_000_000L, "1y")]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-DURATION", "exact-unit-boundaries")]
    public void FriendlyDuration_UsesExactUnitAndSubMillisecondBoundaries(long ticks, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromTicks(ticks).ToFriendlyString());
    }

    [Theory]
    [InlineData(7, "1w")]
    [InlineData(29, "4w1d")]
    [InlineData(30, "1M")]
    [InlineData(364, "12M4d")]
    [InlineData(365, "1y")]
    [InlineData(366, "1y1d")]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-DURATION", "calendar-like-day-boundaries")]
    public void FriendlyDuration_UsesThirtyDayMonthsAndThreeHundredSixtyFiveDayYears(int days, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromDays(days).ToFriendlyString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-DURATION", "composite-unit-order")]
    public void FriendlyDuration_FormatsEveryComponentInStableOrder()
    {
        var duration = new TimeSpan(365 + 60 + 21 + 4, 5, 6, 7).Add(TimeSpan.FromMilliseconds(8));

        Assert.Equal("1y2M3w4d5h6m7s8ms", duration.ToFriendlyString());
    }

    [Theory]
    [InlineData(-1L, "-100ns")]
    [InlineData(-15L, "-2µs")]
    [InlineData(-10_000L, "-1ms")]
    [InlineData(-10_000_000L, "-1s")]
    [InlineData(-36_000_000_000L, "-1h")]
    [InlineData(-864_000_000_000L, "-1d")]
    [InlineData(-25_920_000_000_000L, "-1M")]
    [InlineData(-315_360_000_000_000L, "-1y")]
    [InlineData(long.MinValue, "-29247y1M2w2h48m5s477ms")]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-DURATION", "negative-and-minimum-duration")]
    public void FriendlyDuration_PreservesTheSignAndNaturalUnitsForNegativeValues(long ticks, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromTicks(ticks).ToFriendlyString());
    }
}
