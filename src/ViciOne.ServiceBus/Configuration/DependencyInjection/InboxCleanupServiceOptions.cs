using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration options for inbox cleanup service.</summary>
public abstract class InboxCleanupServiceOptions
{
    /// <summary>The amount of time a message remaining in the Inbox.</summary>
    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>The maximum number of messages to load and remove at a time that meet the criteria.</summary>
    public int QueryMessageLimit { get; set; } = 100;

    /// <summary>Database query timeout for loading/removing messages.</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Delay between each database sweep to cleanup the inbox.</summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);
}


/// <summary>Defines configuration options for inbox cleanup service.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class InboxCleanupServiceOptions<T> :
    InboxCleanupServiceOptions
    where T : class
{
}
