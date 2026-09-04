using System;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Defines configuration options for quartz endpoint.
/// </summary>
public class QuartzEndpointOptions
{
    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int? PrefetchCount { get; set; } = 32;
    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }
    /// <summary>
    /// Gets or sets the queue name value.
    /// </summary>
    public string QueueName { get; set; } = "quartz";
    /// <summary>
    /// Gets or sets the time zone resolver value.
    /// </summary>
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }
}
