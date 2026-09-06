using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration options for job consumer.</summary>
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

    /// <summary>Sets heartbeat interval.</summary>
    /// <param name="d">The <c>d</c> value.</param>
    /// <param name="h">The <c>h</c> value.</param>
    /// <param name="m">The <c>m</c> value.</param>
    /// <param name="s">The <c>s</c> value.</param>
    /// <param name="ms">The ms.</param>
    /// <returns>The job consumer options produced by the operation.</returns>
    public JobConsumerOptions SetHeartbeatInterval(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        HeartbeatInterval = value;

        return this;
    }

    /// <summary>Sets heartbeat interval.</summary>
    /// <param name="interval">The interval.</param>
    /// <returns>The job consumer options produced by the operation.</returns>
    public JobConsumerOptions SetHeartbeatInterval(TimeSpan interval)
    {
        HeartbeatInterval = interval;

        return this;
    }

    /// <summary>Sets rejected job delay.</summary>
    /// <param name="d">The <c>d</c> value.</param>
    /// <param name="h">The <c>h</c> value.</param>
    /// <param name="m">The <c>m</c> value.</param>
    /// <param name="s">The <c>s</c> value.</param>
    /// <param name="ms">The ms.</param>
    /// <returns>The job consumer options produced by the operation.</returns>
    public JobConsumerOptions SetRejectedJobDelay(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        RejectedJobDelay = value;

        return this;
    }

    /// <summary>Sets rejected job delay.</summary>
    /// <param name="interval">The interval.</param>
    /// <returns>The job consumer options produced by the operation.</returns>
    public JobConsumerOptions SetRejectedJobDelay(TimeSpan interval)
    {
        RejectedJobDelay = interval;

        return this;
    }

    /// <summary>Sets time provider.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <returns>The job consumer options produced by the operation.</returns>
    public JobConsumerOptions SetTimeProvider(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        return this;
    }
}
