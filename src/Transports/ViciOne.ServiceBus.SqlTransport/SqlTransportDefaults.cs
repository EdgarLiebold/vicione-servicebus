using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Provides the SQL transport's internal queue-lifetime defaults.</summary>
internal static class SqlTransportDefaults
{
    /// <summary>Gets how long faulted messages remain in an error queue.</summary>
    internal static TimeSpan ErrorQueueTimeToLive { get; } = TimeSpan.FromDays(14);

    /// <summary>Gets the idle period after which a temporary queue is removed.</summary>
    internal static TimeSpan TemporaryQueueAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
}
