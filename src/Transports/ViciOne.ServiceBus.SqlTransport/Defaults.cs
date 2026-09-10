using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines default values for the containing transport.</summary>
public static class Defaults
{
    /// <summary>Gets the lock duration.</summary>
    public static TimeSpan LockDuration { get; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets the default message time to live.</summary>
    public static TimeSpan DefaultMessageTimeToLive { get; } = TimeSpan.FromDays(365 + 1);
    /// <summary>Gets the error queue time to live.</summary>
    public static TimeSpan ErrorQueueTimeToLive { get; } = TimeSpan.FromDays(14);

    /// <summary>Gets the auto delete on idle.</summary>
    public static TimeSpan AutoDeleteOnIdle { get; } = TimeSpan.FromDays(427);
    /// <summary>Gets the temporary auto delete on idle.</summary>
    public static TimeSpan TemporaryAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets the max auto renew duration.</summary>
    public static TimeSpan MaxAutoRenewDuration { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets the session idle timeout.</summary>
    public static TimeSpan SessionIdleTimeout { get; } = TimeSpan.FromSeconds(10);
    /// <summary>Gets the shutdown timeout.</summary>
    public static TimeSpan ShutdownTimeout { get; } = TimeSpan.FromMilliseconds(100);
}
