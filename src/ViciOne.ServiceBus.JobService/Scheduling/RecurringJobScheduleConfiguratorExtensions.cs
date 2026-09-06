using System;
using System.Globalization;
using System.Linq;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Creates calendar and interval schedules for recurring jobs.</summary>
public static class RecurringJobScheduleConfiguratorExtensions
{
    /// <summary>Runs the job once per day at the specified local time.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="hour">The local hour from 0 through 23.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a time component is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator DailyAt(
        this IRecurringJobScheduleConfigurator configurator,
        int hour,
        int minute = 0,
        int second = 0)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTime(hour, minute, second);

        configurator.CronExpression = string.Create(CultureInfo.InvariantCulture, $"{second} {minute} {hour} ? * *");
        return configurator;
    }

    /// <summary>Runs the job on each specified day of the week at the specified local time.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="hour">The local hour from 0 through 23.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <param name="days">One or more distinct days on which the job may run.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when no day is supplied.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a time component or day is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator OnDaysAt(
        this IRecurringJobScheduleConfigurator configurator,
        int hour,
        int minute = 0,
        int second = 0,
        params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTime(hour, minute, second);

        if (days is null || days.Length == 0)
            throw new ArgumentException("At least one day of the week must be specified.", nameof(days));

        configurator.CronExpression = string.Create(CultureInfo.InvariantCulture, $"{second} {minute} {hour} ? * {FormatDays(days)}");
        return configurator;
    }

    /// <summary>Runs the job at a fixed hourly interval, optionally restricted to selected days.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="interval">The number of hours between occurrences, from 1 through 23.</param>
    /// <param name="startingHour">The first local hour in each day from which interval stepping begins.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <param name="days">The days on which the interval applies, or an empty array for every day.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval, a time component, or a day is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator EveryHours(
        this IRecurringJobScheduleConfigurator configurator,
        int interval,
        int startingHour = 0,
        int minute = 0,
        int second = 0,
        params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateInterval(interval, 23);
        ValidateTime(startingHour, minute, second);

        configurator.CronExpression = string.Create(
            CultureInfo.InvariantCulture,
            $"{second} {minute} {startingHour}/{interval} * * {FormatDays(days)}");
        return configurator;
    }

    /// <summary>Runs the job at a fixed minute interval, optionally restricted to an hour and selected days.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="interval">The number of minutes between occurrences, from 1 through 59.</param>
    /// <param name="startingMinute">The first minute in each selected hour from which interval stepping begins.</param>
    /// <param name="hour">The local hour to restrict the interval to, or <see langword="null" /> for every hour.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <param name="days">The days on which the interval applies, or an empty array for every day.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval, a time component, or a day is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator EveryMinutes(
        this IRecurringJobScheduleConfigurator configurator,
        int interval,
        int startingMinute = 0,
        int? hour = null,
        int second = 0,
        params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateInterval(interval, 59);
        ValidateMinute(startingMinute);
        ValidateSecond(second);
        if (hour.HasValue)
            ValidateHour(hour.Value);

        string hourField = hour?.ToString(CultureInfo.InvariantCulture) ?? "*";
        configurator.CronExpression = string.Create(
            CultureInfo.InvariantCulture,
            $"{second} {startingMinute}/{interval} {hourField} * * {FormatDays(days)}");
        return configurator;
    }

    /// <summary>Runs the job at a fixed second interval, optionally restricted to a minute, hour, and selected days.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="interval">The number of seconds between occurrences, from 1 through 59.</param>
    /// <param name="startingSecond">The first second in each selected minute from which interval stepping begins.</param>
    /// <param name="minute">The local minute to restrict the interval to, or <see langword="null" /> for every minute.</param>
    /// <param name="hour">The local hour to restrict the interval to, or <see langword="null" /> for every hour.</param>
    /// <param name="days">The days on which the interval applies, or an empty array for every day.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval, a time component, or a day is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator EverySeconds(
        this IRecurringJobScheduleConfigurator configurator,
        int interval,
        int startingSecond = 0,
        int? minute = null,
        int? hour = null,
        params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateInterval(interval, 59);
        ValidateSecond(startingSecond);
        if (minute.HasValue)
            ValidateMinute(minute.Value);
        if (hour.HasValue)
            ValidateHour(hour.Value);

        string minuteField = minute?.ToString(CultureInfo.InvariantCulture) ?? "*";
        string hourField = hour?.ToString(CultureInfo.InvariantCulture) ?? "*";
        configurator.CronExpression = string.Create(
            CultureInfo.InvariantCulture,
            $"{startingSecond}/{interval} {minuteField} {hourField} * * {FormatDays(days)}");
        return configurator;
    }

    /// <summary>Runs the job once per week on the specified day and local time.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="dayOfWeek">The day on which the job runs.</param>
    /// <param name="hour">The local hour from 0 through 23.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the day or a time component is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator WeeklyOn(
        this IRecurringJobScheduleConfigurator configurator,
        DayOfWeek dayOfWeek,
        int hour,
        int minute = 0,
        int second = 0)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTime(hour, minute, second);
        ValidateDay(dayOfWeek, nameof(dayOfWeek));

        configurator.CronExpression = string.Create(CultureInfo.InvariantCulture, $"{second} {minute} {hour} ? * {(int)dayOfWeek + 1}");
        return configurator;
    }

    /// <summary>Runs the job once per month on the specified day and local time.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="dayOfMonth">The calendar day from 1 through 31.</param>
    /// <param name="hour">The local hour from 0 through 23.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the calendar day or a time component is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator MonthlyOn(
        this IRecurringJobScheduleConfigurator configurator,
        int dayOfMonth,
        int hour,
        int minute = 0,
        int second = 0)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateDayOfMonth(dayOfMonth);
        ValidateTime(hour, minute, second);

        configurator.CronExpression = string.Create(CultureInfo.InvariantCulture, $"{second} {minute} {hour} {dayOfMonth} * ?");
        return configurator;
    }

    /// <summary>Runs the job once per year on the specified calendar date and local time.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="month">The month from 1 through 12.</param>
    /// <param name="dayOfMonth">A valid day in <paramref name="month" />; February 29 is supported for leap years.</param>
    /// <param name="hour">The local hour from 0 through 23.</param>
    /// <param name="minute">The local minute from 0 through 59.</param>
    /// <param name="second">The local second from 0 through 59.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the calendar date or a time component is outside its valid range.</exception>
    public static IRecurringJobScheduleConfigurator YearlyOn(
        this IRecurringJobScheduleConfigurator configurator,
        int month,
        int dayOfMonth,
        int hour,
        int minute = 0,
        int second = 0)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateMonth(month);
        ValidateDayOfMonth(month, dayOfMonth);
        ValidateTime(hour, minute, second);

        configurator.CronExpression = string.Create(CultureInfo.InvariantCulture, $"{second} {minute} {hour} {dayOfMonth} {month} ?");
        return configurator;
    }

    /// <summary>Evaluates the schedule in the specified time zone.</summary>
    /// <param name="configurator">The recurring schedule to configure.</param>
    /// <param name="timeZone">The time zone whose identifier is stored with the schedule.</param>
    /// <returns>The supplied configurator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
    public static IRecurringJobScheduleConfigurator InTimeZone(
        this IRecurringJobScheduleConfigurator configurator,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(timeZone);

        configurator.TimeZoneId = timeZone.Id;
        return configurator;
    }

    static string FormatDays(DayOfWeek[]? days)
    {
        if (days is null || days.Length == 0)
            return "*";

        foreach (DayOfWeek day in days)
            ValidateDay(day, nameof(days));

        return string.Join(',', days.Distinct().Select(static day => ((int)day + 1).ToString(CultureInfo.InvariantCulture)));
    }

    static void ValidateTime(int hour, int minute, int second)
    {
        ValidateHour(hour);
        ValidateMinute(minute);
        ValidateSecond(second);
    }

    static void ValidateHour(int hour)
    {
        if (hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), hour, "The hour must be between 0 and 23.");
    }

    static void ValidateMinute(int minute)
    {
        if (minute is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(minute), minute, "The minute must be between 0 and 59.");
    }

    static void ValidateSecond(int second)
    {
        if (second is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(second), second, "The second must be between 0 and 59.");
    }

    static void ValidateInterval(int interval, int maximum)
    {
        if (interval < 1 || interval > maximum)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, $"The interval must be between 1 and {maximum}.");
    }

    static void ValidateDay(DayOfWeek day, string parameterName)
    {
        if (!Enum.IsDefined(day))
            throw new ArgumentOutOfRangeException(parameterName, day, "The value must identify a day of the week.");
    }

    static void ValidateDayOfMonth(int dayOfMonth)
    {
        if (dayOfMonth is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), dayOfMonth, "The day of the month must be between 1 and 31.");
    }

    static void ValidateMonth(int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month must be between 1 and 12.");
    }

    static void ValidateDayOfMonth(int month, int dayOfMonth)
    {
        int maximum = DateTime.DaysInMonth(2000, month);
        if (dayOfMonth < 1 || dayOfMonth > maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dayOfMonth),
                dayOfMonth,
                $"The day must be between 1 and {maximum} for month {month}.");
        }
    }
}
