using System;
using System.Linq;

namespace ViciOne.ServiceBus.JobService.Scheduling;

/// <summary>Resolves platform time-zone identifiers and preserves chronological order across ambiguous local times.</summary>
internal static class TimeZoneResolver
{
    /// <summary>Converts an instant into the target time zone.</summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="timeZoneInfo">The time zone info.</param>
    /// <returns>The converted time.</returns>
    public static DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
    {
        return TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }

    /// <summary>Returns the target time zone's UTC offset at the supplied instant.</summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="timeZoneInfo">The time zone info.</param>
    /// <returns>The utc offset.</returns>
    public static TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
    {
        return timeZoneInfo.GetUtcOffset(dateTimeOffset);
    }

    /// <summary>Returns the earlier chronological offset when a local clock value is ambiguous.</summary>
    /// <param name="dateTime">The local clock value represented with an offset.</param>
    /// <param name="timeZoneInfo">The time zone whose transition rules are applied.</param>
    /// <returns>The larger ambiguous offset, or the zone's ordinary offset when the value is unambiguous.</returns>
    public static TimeSpan GetAmbiguousTimeUtcOffset(DateTimeOffset dateTime, TimeZoneInfo timeZoneInfo)
    {
        // During a backward clock transition, the larger offset selects the first chronological
        // occurrence of an ambiguous wall-clock value and preserves forward scheduling order.
        DateTime wallTime = dateTime.DateTime;

        var offset = timeZoneInfo.IsAmbiguousTime(wallTime)
            ? timeZoneInfo.GetAmbiguousTimeOffsets(wallTime).Max()
            : timeZoneInfo.GetUtcOffset(wallTime);

        return offset;
    }

    /// <summary>Resolves a platform, IANA, Windows, or application-defined time-zone identifier.</summary>
    /// <param name="id">The time-zone identifier to resolve.</param>
    /// <param name="customResolver">An optional owner-scoped resolver used after platform identifier conversion.</param>
    /// <returns>The matching time zone.</returns>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty or contains only white-space characters.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id" /> is <see langword="null" />.</exception>
    /// <exception cref="TimeZoneNotFoundException">No platform conversion or configured resolver recognizes <paramref name="id" />.</exception>
    public static TimeZoneInfo FindTimeZoneById(string id, Func<string, TimeZoneInfo?>? customResolver = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (TryFindSystemTimeZone(id, out TimeZoneInfo? timeZone, out Exception? platformFailure))
            return timeZone;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out string? windowsId)
            && TryFindSystemTimeZone(windowsId, out timeZone, out _))
            return timeZone;

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out string? ianaId)
            && TryFindSystemTimeZone(ianaId, out timeZone, out _))
            return timeZone;

        timeZone = customResolver?.Invoke(id);
        return timeZone ?? throw new TimeZoneNotFoundException(
            $"The time zone '{id}' could not be resolved by the platform, identifier conversion, or the configured resolver.",
            platformFailure);
    }

    static bool TryFindSystemTimeZone(string id, out TimeZoneInfo timeZone, out Exception? failure)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            failure = null;
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            timeZone = null!;
            failure = exception;
            return false;
        }
    }
}
