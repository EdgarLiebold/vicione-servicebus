using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures lifecycle timing and instance metadata shared by local job consumers.</summary>
public sealed class JobConsumerOptions :
    IOptions,
    ISpecification
{
    /// <summary>Initializes a new instance.</summary>
    public JobConsumerOptions()
    {
        HeartbeatInterval = TimeSpan.FromMinutes(1);
        RejectedJobDelay = TimeSpan.FromSeconds(3);
        TimeProvider = TimeProvider.System;
    }

    /// <summary>Gets or sets the heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval { get; set; }
    /// <summary>Gets or sets the rejected job delay.</summary>
    public TimeSpan RejectedJobDelay { get; set; }
    /// <summary>Gets or sets the time provider.</summary>
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
