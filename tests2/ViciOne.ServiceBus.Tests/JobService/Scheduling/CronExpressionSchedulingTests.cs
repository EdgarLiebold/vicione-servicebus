using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionSchedulingTests
{
    public static TheoryData<string, DateTimeOffset, DateTimeOffset> NextFireCases => new()
    {
        { "0 0 12 15W * ?", Utc(2024, 5, 15, 12, 0), Utc(2024, 6, 14, 12, 0) },
        { "0 0 12 15W * ?", Utc(2024, 8, 15, 12, 0), Utc(2024, 9, 16, 12, 0) },
        { "0 0 12 15W * ?", Utc(2023, 12, 15, 12, 0), Utc(2024, 1, 15, 12, 0) },
        { "0 0 12 31W * ?", Utc(2025, 1, 31, 12, 0), Utc(2025, 2, 28, 12, 0) },
        { "0 0 12 LW * ?", Utc(2023, 2, 28, 12, 0), Utc(2023, 3, 31, 12, 0) },
        { "0 0 12 L-2 * ?", Utc(2023, 4, 28, 12, 0), Utc(2023, 5, 29, 12, 0) },
        { "0 0 12 ? * 6L", Utc(2023, 6, 24, 12, 0), Utc(2023, 6, 30, 12, 0) },
        { "0 0 12 ? * 6#3", Utc(2023, 7, 21, 12, 0), Utc(2023, 8, 18, 12, 0) },
        { "0 0 12 ? * 2/2", Utc(2023, 9, 5, 12, 0), Utc(2023, 9, 6, 12, 0) },
        { "0 0 12 1W * ?", Utc(2023, 10, 1, 12, 0), Utc(2023, 10, 2, 12, 0) },
    };

    [Theory]
    [MemberData(nameof(NextFireCases))]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "next-fire-shapes")]
    public void NextFireTime_MatchesThePublishedCalendarContract(
        string text,
        DateTimeOffset after,
        DateTimeOffset expected)
    {
        var expression = UtcExpression(text);

        Assert.Equal(expected, expression.GetTimeAfter(after));
    }

    [Theory]
    [InlineData("LW-2", 27)]
    [InlineData("LW-5", 24)]
    [InlineData("LW-7", 22)]
    [InlineData("LW-28", 1)]
    [InlineData("LW-29", 1)]
    [InlineData("LW-30", 1)]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "last-weekday-offset")]
    public void LastWeekdayOffset_IsClampedAtTheFirstDay(string dayField, int expectedDay)
    {
        var expression = UtcExpression($"0 15 10 {dayField} * ? 2010");

        foreach (var day in Enumerable.Range(1, 31))
            Assert.Equal(day == expectedDay, expression.IsSatisfiedBy(Utc(2010, 10, day, 10, 15)));
    }

    [Theory]
    [InlineData("0 15 23 * * ?", "2005-06-01T23:16:00+00:00", "2005-06-02T23:15:00+00:00")]
    [InlineData("0 55 15 1 * ?", "2007-12-01T23:59:59+00:00", "2008-01-01T15:55:00+00:00")]
    [InlineData("0/5 * * * * ?", "2005-06-01T01:59:55+00:00", "2005-06-01T02:00:00+00:00")]
    [InlineData("* * * * * ?", "2005-06-01T01:59:55+00:00", "2005-06-01T01:59:56+00:00")]
    [InlineData("* * 1 * * ?", "2005-07-31T22:59:57+00:00", "2005-08-01T01:00:00+00:00")]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "calendar-boundaries")]
    public void NextFireTime_CrossesCalendarBoundaries(
        string text,
        string afterText,
        string expectedText)
    {
        var expression = UtcExpression(text);

        Assert.Equal(
            DateTimeOffset.Parse(expectedText, System.Globalization.CultureInfo.InvariantCulture),
            expression.GetTimeAfter(DateTimeOffset.Parse(afterText, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "missing-day-skips-month")]
    public void DayTwentyNine_SkipsFebruaryAndCrossesTheYear()
    {
        var expression = UtcExpression("0 0 0 29 * ?");

        Assert.Equal(Utc(2009, 3, 29, 0, 0), expression.GetTimeAfter(Utc(2009, 1, 30, 0, 0)));
        Assert.Equal(Utc(2010, 1, 29, 0, 0), expression.GetTimeAfter(Utc(2009, 12, 30, 0, 0)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "last-day-offset")]
    public void LastDayOffsets_MatchOnlyTheirCalculatedDates()
    {
        var twoDaysBefore = UtcExpression("0 15 10 L-2 * ? 2010");
        Assert.True(twoDaysBefore.IsSatisfiedBy(Utc(2010, 10, 29, 10, 15)));
        Assert.False(twoDaysBefore.IsSatisfiedBy(Utc(2010, 10, 28, 10, 15)));

        var fiveDaysBeforeNearestWeekday = UtcExpression("0 15 10 L-5W * ? 2010");
        Assert.True(fiveDaysBeforeNearestWeekday.IsSatisfiedBy(Utc(2010, 10, 26, 10, 15)));

        var oneDayBefore = UtcExpression("0 15 10 L-1 * ? 2010");
        Assert.True(oneDayBefore.IsSatisfiedBy(Utc(2010, 10, 30, 10, 15)));

        var nearestWeekday = UtcExpression("0 15 10 L-1W * ? 2010");
        Assert.True(nearestWeekday.IsSatisfiedBy(Utc(2010, 10, 29, 10, 15)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "nearest-weekday")]
    public void NearestWeekday_NeverSchedulesAWeekend()
    {
        var expression = UtcExpression("0 5 13 5W 1-12 ?");

        DateTimeOffset first = AssertNext(expression, Utc(2009, 3, 8, 0, 0));
        DateTimeOffset second = AssertNext(expression, first);

        Assert.Equal(Utc(2009, 4, 6, 13, 5), first);
        Assert.Equal(Utc(2009, 5, 5, 13, 5), second);
        DayOfWeek[] weekend = [DayOfWeek.Saturday, DayOfWeek.Sunday];
        Assert.DoesNotContain(first.DayOfWeek, weekend);
        Assert.DoesNotContain(second.DayOfWeek, weekend);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "year-crossing-remains-defined")]
    public void WeekdayExpression_CrossesTheYearWithoutLosingItsNextOccurrence()
    {
        var expression = UtcExpression("0 12 4 ? * 3");

        DateTimeOffset next = AssertNext(expression, Utc(2007, 12, 28, 0, 0));

        Assert.Equal(2008, next.Year);
        Assert.Equal(DayOfWeek.Tuesday, next.DayOfWeek);
        Assert.Equal(new TimeOnly(4, 12), TimeOnly.FromDateTime(next.UtcDateTime));
    }

    private static DateTimeOffset AssertNext(CronExpression expression, DateTimeOffset after) =>
        expression.GetNextValidTimeAfter(after)
        ?? throw new Xunit.Sdk.XunitException($"No next fire time after {after:O}.");

    private static CronExpression UtcExpression(string text) =>
        new(text) { TimeZone = TimeZoneInfo.Utc };

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
