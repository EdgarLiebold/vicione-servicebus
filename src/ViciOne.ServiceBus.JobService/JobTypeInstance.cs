using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Tracks the heartbeat, utilization, and distribution metadata of one job-service instance.</summary>
public sealed class JobTypeInstance
{
    /// <summary>Gets or sets the instant of the latest availability heartbeat.</summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>Gets or sets the instant at which this instance most recently received an allocation.</summary>
    public DateTimeOffset? Used { get; set; }

    /// <summary>Gets or sets instance metadata available to distribution strategies.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
}
