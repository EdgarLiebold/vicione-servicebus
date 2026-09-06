using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class RecurringJobScheduleConfiguratorTests
{
    [Theory]
    [InlineData(0, 0, 0, "0 0 0 ? * *")]
    [InlineData(23, 59, 58, "58 59 23 ? * *")]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "daily-schedule-preserves-every-time-component")]
    public void DailyAt_PreservesEveryTimeComponent(int hour, int minute, int second, string expected)
    {
        var schedule = new JobScheduleInfo();

        IRecurringJobScheduleConfigurator result = schedule.DailyAt(hour, minute, second);

        Assert.Same(schedule, result);
        Assert.Equal(expected, schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "selected-days-and-time")]
    public void OnDaysAt_UsesEveryDistinctSelectedDayAndTimeComponent()
    {
        var schedule = new JobScheduleInfo();

        schedule.OnDaysAt(21, 43, 17, DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Monday);

        Assert.Equal("17 43 21 ? * 2,6", schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "hourly-interval-all-parameters")]
    public void EveryHours_UsesTheIntervalAnchorTimeAndSelectedDays()
    {
        var schedule = new JobScheduleInfo();

        schedule.EveryHours(4, 2, 15, 30, DayOfWeek.Tuesday, DayOfWeek.Thursday);

        Assert.Equal("30 15 2/4 * * 3,5", schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "minute-interval-all-parameters")]
    public void EveryMinutes_UsesTheIntervalAnchorTimeAndSelectedDays()
    {
        var schedule = new JobScheduleInfo();

        schedule.EveryMinutes(10, 5, 12, 20, DayOfWeek.Wednesday);

        Assert.Equal("20 5/10 12 * * 4", schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "second-interval-all-parameters")]
    public void EverySeconds_UsesTheIntervalAnchorTimeAndSelectedDays()
    {
        var schedule = new JobScheduleInfo();

        schedule.EverySeconds(5, 2, 30, 9, DayOfWeek.Saturday, DayOfWeek.Sunday);

        Assert.Equal("2/5 30 9 * * 7,1", schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Theory]
    [InlineData(IntervalUnit.Hours, "0 0 0/1 * * *")]
    [InlineData(IntervalUnit.Minutes, "0 0/1 * * * *")]
    [InlineData(IntervalUnit.Seconds, "0/1 * * * * *")]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "interval-defaults")]
    public void IntervalSchedule_DefaultsLeaveCalendarFieldsUnrestricted(IntervalUnit unit, string expected)
    {
        var schedule = new JobScheduleInfo();

        _ = unit switch
        {
            IntervalUnit.Hours => schedule.EveryHours(1),
            IntervalUnit.Minutes => schedule.EveryMinutes(1),
            IntervalUnit.Seconds => schedule.EverySeconds(1),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
        };

        Assert.Equal(expected, schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "calendar-periods-preserve-seconds")]
    public void CalendarPeriodSchedules_PreserveEverySuppliedComponent()
    {
        var weekly = new JobScheduleInfo();
        var monthly = new JobScheduleInfo();
        var yearly = new JobScheduleInfo();

        weekly.WeeklyOn(DayOfWeek.Thursday, 14, 15, 16);
        monthly.MonthlyOn(28, 14, 15, 16);
        yearly.YearlyOn(11, 28, 14, 15, 16);

        Assert.Equal("16 15 14 ? * 5", weekly.CronExpression);
        Assert.Equal("16 15 14 28 * ?", monthly.CronExpression);
        Assert.Equal("16 15 14 28 11 ?", yearly.CronExpression);
        Assert.Empty(weekly.Validate());
        Assert.Empty(monthly.Validate());
        Assert.Empty(yearly.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "time-zone")]
    public void InTimeZone_StoresThePlatformTimeZoneIdentifier()
    {
        var schedule = new JobScheduleInfo();

        IRecurringJobScheduleConfigurator result = schedule.InTimeZone(TimeZoneInfo.Utc);

        Assert.Same(schedule, result);
        Assert.Equal(TimeZoneInfo.Utc.Id, schedule.TimeZoneId);
    }

    [Theory]
    [InlineData(IntervalUnit.Hours, 0)]
    [InlineData(IntervalUnit.Hours, 24)]
    [InlineData(IntervalUnit.Minutes, 0)]
    [InlineData(IntervalUnit.Minutes, 60)]
    [InlineData(IntervalUnit.Seconds, 0)]
    [InlineData(IntervalUnit.Seconds, 60)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "interval-range")]
    public void IntervalOutsideCronRange_IsRejected(IntervalUnit unit, int interval)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = unit switch
            {
                IntervalUnit.Hours => schedule.EveryHours(interval),
                IntervalUnit.Minutes => schedule.EveryMinutes(interval),
                IntervalUnit.Seconds => schedule.EverySeconds(interval),
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
            };
        });

        Assert.Equal("interval", exception.ParamName);
        Assert.Equal(interval, exception.ActualValue);
    }

    [Theory]
    [InlineData(-1, 0, 0, "hour")]
    [InlineData(24, 0, 0, "hour")]
    [InlineData(0, -1, 0, "minute")]
    [InlineData(0, 60, 0, "minute")]
    [InlineData(0, 0, -1, "second")]
    [InlineData(0, 0, 60, "second")]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "clock-range")]
    public void ClockComponentOutsideItsRange_IsRejected(int hour, int minute, int second, string parameterName)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => schedule.DailyAt(hour, minute, second));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "day-of-month-range")]
    public void DayOfMonthOutsideItsRange_IsRejected(int dayOfMonth)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => schedule.MonthlyOn(dayOfMonth, 0));

        Assert.Equal("dayOfMonth", exception.ParamName);
        Assert.Equal(dayOfMonth, exception.ActualValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "month-range")]
    public void MonthOutsideItsRange_IsRejected(int month)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => schedule.YearlyOn(month, 1, 0));

        Assert.Equal("month", exception.ParamName);
        Assert.Equal(month, exception.ActualValue);
    }

    [Theory]
    [InlineData(2, 30)]
    [InlineData(4, 31)]
    [InlineData(11, 31)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "yearly-calendar-date")]
    public void YearlyOn_RejectsADayThatDoesNotExistInTheSelectedMonth(int month, int dayOfMonth)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => schedule.YearlyOn(month, dayOfMonth, 0));

        Assert.Equal("dayOfMonth", exception.ParamName);
        Assert.Equal(dayOfMonth, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "leap-day-is-valid")]
    public void YearlyOn_AcceptsLeapDay()
    {
        var schedule = new JobScheduleInfo();

        schedule.YearlyOn(2, 29, 0);

        Assert.Equal("0 0 0 29 2 ?", schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "empty-time-zone-identifier")]
    public void ScheduleValidation_RejectsAnEmptyTimeZoneIdentifier()
    {
        var schedule = new JobScheduleInfo
        {
            CronExpression = "0 0 0 ? * *",
            TimeZoneId = " "
        };

        ValidationResult failure = Assert.Single(schedule.Validate());

        Assert.Contains("TimeZoneId", failure.Key, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "days-required")]
    public void OnDaysAt_RequiresAtLeastOneDay()
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentException>(() => schedule.OnDaysAt(0));

        Assert.Equal("days", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "defined-day-of-week")]
    public void UndefinedDayOfWeek_IsRejected(int rawDay)
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => schedule.WeeklyOn((DayOfWeek)rawDay, 0));

        Assert.Equal("dayOfWeek", exception.ParamName);
        Assert.Equal((DayOfWeek)rawDay, exception.ActualValue);
    }

    [Theory]
    [InlineData(ScheduleOperation.DailyAt)]
    [InlineData(ScheduleOperation.OnDaysAt)]
    [InlineData(ScheduleOperation.EveryHours)]
    [InlineData(ScheduleOperation.EveryMinutes)]
    [InlineData(ScheduleOperation.EverySeconds)]
    [InlineData(ScheduleOperation.WeeklyOn)]
    [InlineData(ScheduleOperation.MonthlyOn)]
    [InlineData(ScheduleOperation.YearlyOn)]
    [InlineData(ScheduleOperation.InTimeZone)]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "null-receiver")]
    public void EveryExtension_RejectsANullConfigurator(ScheduleOperation operation)
    {
        IRecurringJobScheduleConfigurator schedule = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = operation switch
            {
                ScheduleOperation.DailyAt => schedule.DailyAt(0),
                ScheduleOperation.OnDaysAt => schedule.OnDaysAt(0, days: [DayOfWeek.Monday]),
                ScheduleOperation.EveryHours => schedule.EveryHours(1),
                ScheduleOperation.EveryMinutes => schedule.EveryMinutes(1),
                ScheduleOperation.EverySeconds => schedule.EverySeconds(1),
                ScheduleOperation.WeeklyOn => schedule.WeeklyOn(DayOfWeek.Monday, 0),
                ScheduleOperation.MonthlyOn => schedule.MonthlyOn(1, 0),
                ScheduleOperation.YearlyOn => schedule.YearlyOn(1, 1, 0),
                ScheduleOperation.InTimeZone => schedule.InTimeZone(TimeZoneInfo.Utc),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
            };
        });

        Assert.Equal("configurator", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "null-time-zone")]
    public void InTimeZone_RejectsANullTimeZone()
    {
        var schedule = new JobScheduleInfo();

        var exception = Assert.Throws<ArgumentNullException>(() => schedule.InTimeZone(null!));

        Assert.Equal("timeZone", exception.ParamName);
    }

    public enum IntervalUnit
    {
        Hours,
        Minutes,
        Seconds,
    }

    public enum ScheduleOperation
    {
        DailyAt,
        OnDaysAt,
        EveryHours,
        EveryMinutes,
        EverySeconds,
        WeeklyOn,
        MonthlyOn,
        YearlyOn,
        InTimeZone,
    }
}
