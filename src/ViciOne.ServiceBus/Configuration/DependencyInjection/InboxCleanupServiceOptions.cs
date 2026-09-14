using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls duplicate-detection retention and cleanup query behavior for an inbox.</summary>
public abstract class InboxCleanupServiceOptions
{
    /// <summary>Gets or sets how long an inbox retains a consumed message for duplicate detection.</summary>
    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the maximum number of expired inbox records removed in one query.</summary>
    public int QueryMessageLimit { get; set; } = 100;

    /// <summary>Gets or sets the timeout for each cleanup query.</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the delay between cleanup queries.</summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);
}


/// <summary>Scopes inbox cleanup options to a persistence owner.</summary>
/// <typeparam name="T">The persistence owner that isolates the option instance.</typeparam>
public sealed class InboxCleanupServiceOptions<T> :
    InboxCleanupServiceOptions
    where T : class
{
}
