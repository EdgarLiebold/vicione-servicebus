using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class CronExpressionDaylightSavingTests
{
    private static readonly TimeZoneInfo Eastern = CreateEasternTimeZone();

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-DST", "spring-gap-carries-occurrence")]
    public void SpringGap_CarriesTheMissingOccurrenceIntoTheNextLocalHour()
    {
        var expression = new CronExpression("0 15 2 * * ?") { TimeZone = Eastern };
        var before = Utc(2012, 3, 11, 6, 55);

        DateTimeOffset actual = AssertNext(expression, before);

        Assert.Equal(Utc(2012, 3, 11, 7, 15), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-DST", "autumn-date-not-one-hour-early")]
    public void AutumnTransition_DateScheduleDoesNotFireOneHourEarly()
    {
        var expression = new CronExpression("0 15 15 5 11 ?") { TimeZone = Eastern };

        DateTimeOffset actual = AssertNext(expression, Utc(2012, 11, 4, 0, 0));

        Assert.Equal(Utc(2012, 11, 5, 20, 15), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CRON-DST", "autumn-weekday-not-one-hour-early")]
    public void AutumnTransition_WeekdayScheduleUsesTheStandardOffset()
    {
        var expression = new CronExpression("0 0 0 ? * THU") { TimeZone = Eastern };

        DateTimeOffset actual = AssertNext(expression, Utc(2012, 11, 4, 0, 0));

        Assert.Equal(Utc(2012, 11, 8, 5, 0), actual);
    }

    private static TimeZoneInfo CreateEasternTimeZone()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            2,
            DayOfWeek.Sunday);
        var end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            1,
            DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            DateTime.MinValue.Date,
            DateTime.MaxValue.Date,
            TimeSpan.FromHours(1),
            start,
            end);

        return TimeZoneInfo.CreateCustomTimeZone(
            "ViciOne.Testing.Eastern",
            TimeSpan.FromHours(-5),
            "ViciOne Testing Eastern",
            "ViciOne Testing Eastern Standard Time",
            "ViciOne Testing Eastern Daylight Time",
            [rule]);
    }

    private static DateTimeOffset AssertNext(CronExpression expression, DateTimeOffset after) =>
        expression.GetNextValidTimeAfter(after)
        ?? throw new Xunit.Sdk.XunitException($"No next fire time after {after:O}.");

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
