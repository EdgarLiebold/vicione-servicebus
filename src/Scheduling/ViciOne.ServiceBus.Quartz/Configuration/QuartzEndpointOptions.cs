using System;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Configures the bus endpoint that accepts Quartz scheduling commands.</summary>
public sealed class QuartzEndpointOptions
{
    /// <summary>Gets or sets the transport prefetch count; <see langword="null"/> leaves it provider-defined.</summary>
    public int? PrefetchCount { get; set; } = 32;
    /// <summary>Gets or sets the maximum number of concurrently processed scheduling commands.</summary>
    public int? ConcurrentMessageLimit { get; set; }
    /// <summary>Gets or sets the scheduling endpoint queue name.</summary>
    public string QueueName { get; set; } = "quartz";
    /// <summary>Gets or sets a fallback resolver for time-zone identifiers unavailable to the operating system.</summary>
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }
}
