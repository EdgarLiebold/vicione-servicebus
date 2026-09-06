using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.JobService.Scheduling;

/// <summary>Provides utility operations for time zone.</summary>
public static class TimeZoneUtil
{
    static readonly Dictionary<string, string> TimeZoneIdAliases = new Dictionary<string, string>();

    static TimeZoneUtil()
    {
        TimeZoneIdAliases["UTC"] = "Coordinated Universal Time";
        TimeZoneIdAliases["Coordinated Universal Time"] = "UTC";

        TimeZoneIdAliases["Central European Standard Time"] = "CET";
        TimeZoneIdAliases["CET"] = "Central European Standard Time";

        TimeZoneIdAliases["Eastern Standard Time"] = "US/Eastern";
        TimeZoneIdAliases["US/Eastern"] = "Eastern Standard Time";

        TimeZoneIdAliases["Central Standard Time"] = "US/Central";
        TimeZoneIdAliases["US/Central"] = "Central Standard Time";

        TimeZoneIdAliases["US Central Standard Time"] = "US/Indiana-Stark";
        TimeZoneIdAliases["US/Indiana-Stark"] = "US Central Standard Time";

        TimeZoneIdAliases["Mountain Standard Time"] = "US/Mountain";
        TimeZoneIdAliases["US/Mountain"] = "Mountain Standard Time";

        TimeZoneIdAliases["US Mountain Standard Time"] = "US/Arizona";
        TimeZoneIdAliases["US/Arizona"] = "US Mountain Standard Time";

        TimeZoneIdAliases["Pacific Standard Time"] = "US/Pacific";
        TimeZoneIdAliases["US/Pacific"] = "Pacific Standard Time";

        TimeZoneIdAliases["Alaskan Standard Time"] = "US/Alaska";
        TimeZoneIdAliases["US/Alaska"] = "Alaskan Standard Time";

        TimeZoneIdAliases["Hawaiian Standard Time"] = "US/Hawaii";
        TimeZoneIdAliases["US/Hawaii"] = "Hawaiian Standard Time";

        TimeZoneIdAliases["China Standard Time"] = "Asia/Shanghai";
        TimeZoneIdAliases["Asia/Shanghai"] = "China Standard Time";

        TimeZoneIdAliases["Pakistan Standard Time"] = "Asia/Karachi";
        TimeZoneIdAliases["Asia/Karachi"] = "Pakistan Standard Time";
    }

    /// <summary>TimeZoneInfo.ConvertTime is not supported under mono.</summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="timeZoneInfo">The time zone info.</param>
    /// <returns>The converted time.</returns>
    public static DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
    {
        return TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
    }

    /// <summary>TimeZoneInfo.GetUtcOffset(DateTimeOffset) is not supported under mono.</summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="timeZoneInfo">The time zone info.</param>
    /// <returns>The utc offset.</returns>
    public static TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
    {
        return timeZoneInfo.GetUtcOffset(dateTimeOffset);
    }

    /// <summary>Gets ambiguous time utc offset.</summary>
    /// <param name="dateTime">The date time.</param>
    /// <param name="timeZoneInfo">The time zone info.</param>
    /// <returns>The ambiguous time utc offset.</returns>
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

    /// <summary>Tries to find time zone with given id, has ability do some fallbacks when necessary.</summary>
    /// <param name="id">System id of the time zone.</param>
    /// <param name="customResolver">An optional owner-scoped resolver used after platform and alias lookup.</param>
    /// <returns>The matching time zone by id.</returns>
    public static TimeZoneInfo FindTimeZoneById(string id, Func<string, TimeZoneInfo?>? customResolver = null)
    {
        TimeZoneInfo? info = null;
        try
        {
            info = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException ex)
        {
            if (TimeZoneIdAliases.TryGetValue(id, out var aliasedId))
            {
                try
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById(aliasedId);
                }
                catch
                {
                }
            }

            info ??= customResolver?.Invoke(id);
            if (info is null)
            {
                throw new TimeZoneNotFoundException(
                    $"Could not find time zone with id {id}, consider using Quartz.Plugins.TimeZoneConverter for resolving more time zones ids", ex);
            }
        }

        return info;
    }
}
