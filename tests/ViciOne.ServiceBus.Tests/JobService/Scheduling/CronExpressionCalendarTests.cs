using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionCalendarTests
{
    public static TheoryData<string, int[]> DayUnionCases => new()
    {
        { "0 15 10 5/5 * MON 2010", [4, 5, 10, 11, 15, 18, 20, 25, 30] },
        { "0 15 10 3 * MON,THU,FRI 2010", [1, 3, 4, 7, 8, 11, 14, 15, 18, 21, 22, 25, 28, 29] },
        { "0 15 10 1,2,3,4,5,6 * MON,THU,FRI 2010", [1, 2, 3, 4, 5, 6, 7, 8, 11, 14, 15, 18, 21, 22, 25, 28, 29] },
        { "0 15 10 * * MON,THU,FRI 2010", Enumerable.Range(1, 31).ToArray() },
        { "0 15 10 1 * * 2010", Enumerable.Range(1, 31).ToArray() },
    };

    [Theory]
    [MemberData(nameof(DayUnionCases))]
    [RequirementCoverage("REQ-VSB-CRON-MATCHING", "day-of-month-and-week-union")]
    public void DayOfMonthAndDayOfWeek_AreCombinedAsAUnion(string text, int[] expectedDays)
    {
        var expression = UtcExpression(text);
        var expected = expectedDays.ToHashSet();

        foreach (var day in Enumerable.Range(1, 31))
        {
            Assert.Equal(
                expected.Contains(day),
                expression.IsSatisfiedBy(Utc(2010, 10, day, 10, 15)));
        }
    }

    public static TheoryData<string, int[]> JuneScheduleCases => new()
    {
        { "0 0 12 ? * MON-FRI", [1, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 18, 19, 20, 21, 22, 25, 26, 27, 28, 29] },
        { "0 0 12 ? * FRI", [1, 8, 15, 22, 29] },
        { "0 0 12 ? * FRI/2", [1, 15, 29] },
        { "0 0 12 ? * THU,FRI/2", [1, 14, 15, 28, 29] },
        { "0 0 12 L * ?", [30] },
    };

    [Theory]
    [MemberData(nameof(JuneScheduleCases))]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "weekday-calendar")]
    public void JuneSchedule_MatchesTheCompleteExpectedCalendar(string text, int[] expectedDays)
    {
        var expression = UtcExpression(text);
        var actualDays = new List<int>();
        DateTimeOffset cursor = Utc(2007, 6, 1, 11, 0);

        for (var index = 0; index < 40; index++)
        {
            DateTimeOffset? next = expression.GetTimeAfter(cursor);
            Assert.NotNull(next);
            if (next.Value is not { Year: 2007, Month: 6 })
                break;

            if (!actualDays.Contains(next.Value.Day))
                actualDays.Add(next.Value.Day);

            cursor = next.Value;
        }

        Assert.Equal(expectedDays, actualDays);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "third-friday-hour-range")]
    public void ThirdFridayHourRange_MatchesOnlyItsFourHalfHourSamples()
    {
        var expression = UtcExpression("0 30 10-13 ? * FRI#3");
        var start = Utc(2008, 12, 19, 0, 0);

        for (var index = 0; index < 200; index++)
        {
            bool expected = start.DayOfWeek == DayOfWeek.Friday
                && start.Day is > 15 and < 22
                && start.Hour is >= 10 and <= 13
                && start.Minute == 30;

            Assert.Equal(expected, expression.IsSatisfiedBy(start));
            start = start.AddMinutes(30);
        }
    }

    [Theory]
    [InlineData("JAN", 1)]
    [InlineData("FEB", 2)]
    [InlineData("MAR", 3)]
    [InlineData("APR", 4)]
    [InlineData("MAY", 5)]
    [InlineData("JUN", 6)]
    [InlineData("JUL", 7)]
    [InlineData("AUG", 8)]
    [InlineData("SEP", 9)]
    [InlineData("OCT", 10)]
    [InlineData("NOV", 11)]
    [InlineData("DEC", 12)]
    [RequirementCoverage("REQ-VSB-CRON-SCHEDULING", "month-abbreviations")]
    public void MonthAbbreviation_MapsToTheExpectedCalendarField(string abbreviation, int month)
    {
        var expression = new CronExpression($"0 0 0 1 {abbreviation} ? *");

        Assert.Equal([month], expression.GetSet(CronExpressionConstants.Month));
    }

    private static CronExpression UtcExpression(string text) =>
        new(text) { TimeZone = TimeZoneInfo.Utc };

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
