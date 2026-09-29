using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionBoundaryRegressionTests
{
    [Theory]
    [InlineData("! 0 9 ? * MON 2026")]
    [InlineData("0,! 0 9 ? * MON 2026")]
    [RequirementCoverage("REQ-VSB-CRON-VALIDATION", "invalid-token-start-is-rejected-in-fields-and-lists")]
    public void InvalidTokenStart_IsRejectedByEveryValidationEntryPoint(string text)
    {
        FormatException constructorFailure = Assert.Throws<FormatException>(() => new CronExpression(text));
        FormatException validationFailure = Assert.Throws<FormatException>(() => CronExpression.ValidateExpression(text));

        Assert.Equal("Unexpected character: !", constructorFailure.Message);
        Assert.Equal("Unexpected character: !", validationFailure.Message);
        Assert.False(CronExpression.IsValidExpression(text));
    }

    [Theory]
    [InlineData("\u00a0")]
    [InlineData("\u2003")]
    [InlineData("\r\n")]
    [RequirementCoverage("REQ-VSB-CRON-PARSING", "unicode-whitespace-preserves-normalized-schedule")]
    public void UnicodeWhitespace_PreservesCanonicalTextAndNextOccurrence(string separator)
    {
        string text = $"{separator}0{separator}15{separator}9{separator}?{separator}*{separator}mon{separator}2026{separator}";
        var expression = new CronExpression(text) { TimeZone = TimeZoneInfo.Utc };

        Assert.Equal("0 15 9 ? * MON 2026", expression.ToString());
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 9, 15, 0, TimeSpan.Zero),
            expression.GetTimeAfter(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(new DateTimeOffset(2026, 6, 8, 9, 15, 0, TimeSpan.Zero),
            expression.GetTimeAfter(new DateTimeOffset(2026, 6, 1, 9, 15, 0, TimeSpan.Zero)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "day-union-enumerates-month-transition-and-coincident-date-once")]
    public void CombinedCalendar_EnumeratesUnionWithoutSkippingOrDuplicatingDates()
    {
        var expression = new CronExpression("0 0 9 15 * MON 2026") { TimeZone = TimeZoneInfo.Utc };
        DateTimeOffset[] expected =
        [
            Utc(6, 1), Utc(6, 8), Utc(6, 15), Utc(6, 22), Utc(6, 29),
            Utc(7, 6), Utc(7, 13), Utc(7, 15), Utc(7, 20),
        ];
        DateTimeOffset cursor = Utc(5, 31);

        foreach (DateTimeOffset occurrence in expected)
        {
            DateTimeOffset? actual = expression.GetTimeAfter(cursor);
            Assert.Equal(occurrence, actual);
            Assert.True(actual > cursor);
            cursor = actual.Value;
        }

        Assert.Null(expression.GetTimeAfter(Utc(12, 31)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "terminal-instant-has-no-next-occurrence")]
    public void MaximumInstant_ExhaustsTheSupportedYearWithoutOverflow()
    {
        var expression = new CronExpression("0 0 0 1 1 ? 2199") { TimeZone = TimeZoneInfo.Utc };

        Assert.Null(expression.GetTimeAfter(DateTimeOffset.MaxValue));
        Assert.Null(expression.GetNextValidTimeAfter(DateTimeOffset.MaxValue));
        Assert.False(expression.IsSatisfiedBy(DateTimeOffset.MaxValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "minimum-instant-is-unsatisfied-and-finds-first-year")]
    public void MinimumInstant_IsUnsatisfiedAndFindsTheFirstSupportedYear(int inputOffsetHours)
    {
        var expression = new CronExpression("0 0 0 1 1 ? 1970") { TimeZone = TimeZoneInfo.Utc };
        var firstOccurrence = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var minimumInstant = DateTimeOffset.MinValue.ToOffset(TimeSpan.FromHours(inputOffsetHours));

        Assert.False(expression.IsSatisfiedBy(minimumInstant));
        Assert.Equal(firstOccurrence, expression.GetTimeAfter(minimumInstant));
        Assert.Equal(firstOccurrence, expression.GetNextValidTimeAfter(minimumInstant));
        Assert.True(expression.IsSatisfiedBy(firstOccurrence));
    }

    [Theory]
    [InlineData(-10, 9)]
    [InlineData(-14, 13)]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "last-local-year-can-fire-in-next-utc-year")]
    public void LastSupportedLocalYear_RemainsVisibleAcrossTheUtcYearBoundary(int zoneOffsetHours, int utcHour)
    {
        TimeZoneInfo western = TimeZoneInfo.CreateCustomTimeZone(
            $"ViciOne.Cron.LastYear.Western.{zoneOffsetHours}", TimeSpan.FromHours(zoneOffsetHours), "Western", "Western");
        var expression = new CronExpression("59 59 23 31 12 ? 2199") { TimeZone = western };
        var lastOccurrence = new DateTimeOffset(2200, 1, 1, utcHour, 59, 59, TimeSpan.Zero);
        var before = lastOccurrence.AddSeconds(-1);

        Assert.Equal(lastOccurrence, expression.GetTimeAfter(before));
        Assert.Equal(lastOccurrence, expression.GetNextValidTimeAfter(before));
        Assert.True(expression.IsSatisfiedBy(lastOccurrence));
        Assert.Null(expression.GetTimeAfter(lastOccurrence));
        Assert.False(expression.IsSatisfiedBy(lastOccurrence.AddSeconds(1)));
    }

    private static DateTimeOffset Utc(int month, int day) =>
        new(2026, month, day, 9, 0, 0, TimeSpan.Zero);
}
