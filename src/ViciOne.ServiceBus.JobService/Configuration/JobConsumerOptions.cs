using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures lifecycle timing and instance metadata shared by local job consumers.</summary>
public sealed class JobConsumerOptions :
    IOptions,
    ISpecification
{
    /// <summary>Creates options with a one-minute heartbeat and a three-second local rejection delay.</summary>
    public JobConsumerOptions()
    {
        HeartbeatInterval = TimeSpan.FromMinutes(1);
        RejectedJobDelay = TimeSpan.FromSeconds(3);
        TimeProvider = TimeProvider.System;
    }

    /// <summary>Gets or sets the delay between availability heartbeats from a running service instance.</summary>
    public TimeSpan HeartbeatInterval { get; set; }
    /// <summary>Gets or sets the delay before a job rejected during shutdown becomes eligible for rescheduling.</summary>
    public TimeSpan RejectedJobDelay { get; set; }
    /// <summary>Gets or sets the clock used for local execution timeouts and lifecycle timestamps.</summary>
    public TimeProvider TimeProvider { get; set; }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (HeartbeatInterval <= TimeSpan.Zero)
            yield return this.Failure("JobConsumerOptions", "HeartbeatInterval", "Must be > 0");
        if (RejectedJobDelay <= TimeSpan.Zero)
            yield return this.Failure("JobConsumerOptions", "RejectedJobDelay", "Must be > 0");
        if (TimeProvider == null)
            yield return this.Failure("JobConsumerOptions", "TimeProvider", "Must not be null");
    }

}
