using System;
using System.Text;

namespace ViciOne.ServiceBus.Internals;

internal static class TimeSpanExtensions
{
    static readonly TimeSpan _day = TimeSpan.FromDays(1);
    static readonly TimeSpan _hour = TimeSpan.FromHours(1);
    static readonly TimeSpan _month = TimeSpan.FromDays(30);
    static readonly TimeSpan _year = TimeSpan.FromDays(365);

    public static string ToFriendlyString(this TimeSpan ts)
    {
        long signedTicks = ts.Ticks;
        ulong ticks = signedTicks < 0
            ? (ulong)(-(signedTicks + 1)) + 1
            : (ulong)signedTicks;

        string magnitude = FormatMagnitude(ticks);
        return signedTicks < 0 ? "-" + magnitude : magnitude;
    }

    static string FormatMagnitude(ulong ticks)
    {
        if (ticks == (ulong)_month.Ticks)
            return "1M";
        if (ticks == (ulong)_year.Ticks)
            return "1y";
        if (ticks == (ulong)_day.Ticks)
            return "1d";
        if (ticks == (ulong)_hour.Ticks)
            return "1h";
        if (ticks == 0)
            return "-0-";

        var sb = new StringBuilder();
        AppendCalendarUnits(sb, ticks);
        AppendClockUnits(sb, ticks);

        return sb.Length == 0 ? FormatSubMillisecond(ticks) : sb.ToString();
    }

    static void AppendCalendarUnits(StringBuilder sb, ulong ticks)
    {
        ulong totalDays = ticks / (ulong)TimeSpan.TicksPerDay;
        ulong years = totalDays / 365;
        ulong months = totalDays % 365 / 30;
        ulong weeks = totalDays % 365 % 30 / 7;
        ulong days = totalDays % 365 % 30 % 7;

        if (years > 0)
            sb.Append(years).Append("y");

        if (months > 0)
            sb.Append(months).Append("M");

        if (weeks > 0)
            sb.Append(weeks).Append("w");

        if (days > 0)
            sb.Append(days).Append("d");
    }

    static void AppendClockUnits(StringBuilder sb, ulong ticks)
    {
        ulong remainingTicks = ticks % (ulong)TimeSpan.TicksPerDay;
        ulong hours = remainingTicks / (ulong)TimeSpan.TicksPerHour;
        remainingTicks %= (ulong)TimeSpan.TicksPerHour;
        ulong minutes = remainingTicks / (ulong)TimeSpan.TicksPerMinute;
        remainingTicks %= (ulong)TimeSpan.TicksPerMinute;
        ulong seconds = remainingTicks / (ulong)TimeSpan.TicksPerSecond;
        remainingTicks %= (ulong)TimeSpan.TicksPerSecond;
        ulong milliseconds = remainingTicks / (ulong)TimeSpan.TicksPerMillisecond;

        if (hours > 0)
            sb.Append(hours).Append("h");
        if (minutes > 0)
            sb.Append(minutes).Append("m");
        if (seconds > 0)
            sb.Append(seconds).Append("s");
        if (milliseconds > 0)
            sb.Append(milliseconds).Append("ms");
    }

    static string FormatSubMillisecond(ulong ticks)
    {
        ulong nanos = ticks * 100;
        var sb = new StringBuilder();
        if (nanos > 1000)
            sb.Append((nanos + 500) / 1000).Append("\x00B5s");
        else
            sb.Append(nanos).Append("ns");

        return sb.ToString();
    }
}
