using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Provides the SQL transport's internal queue-lifetime defaults.</summary>
internal static class SqlTransportDefaults
{
    internal static int? ToDatabaseAutoDeleteSeconds(TimeSpan? interval)
    {
        if (!interval.HasValue)
            return null;

        if (interval.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The auto-delete interval must be greater than zero.");

        long seconds = interval.Value.Ticks / TimeSpan.TicksPerSecond;
        if (interval.Value.Ticks % TimeSpan.TicksPerSecond != 0)
            seconds++;

        if (seconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The auto-delete interval exceeds the SQL seconds limit.");

        return (int)seconds;
    }

    /// <summary>Gets how long faulted messages remain in an error queue.</summary>
    internal static TimeSpan ErrorQueueTimeToLive { get; } = TimeSpan.FromDays(14);

    /// <summary>Gets the idle period after which a temporary queue is removed.</summary>
    internal static TimeSpan TemporaryQueueAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
}
