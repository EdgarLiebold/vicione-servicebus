using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines configuration options for job consumer.
/// </summary>
public sealed class JobConsumerOptions :
    IOptions,
    ISpecification
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobConsumerOptions()
    {
        HeartbeatInterval = TimeSpan.FromMinutes(1);
        RejectedJobDelay = TimeSpan.FromSeconds(3);
        TimeProvider = TimeProvider.System;
    }

    /// <summary>
    /// Gets or sets the heartbeat interval value.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; }
    /// <summary>
    /// Gets or sets the rejected job delay value.
    /// </summary>
    public TimeSpan RejectedJobDelay { get; set; }
    /// <summary>
    /// Gets or sets the time provider value.
    /// </summary>
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

    /// <summary>
    /// Sets heartbeat interval.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <param name="h">The h value.</param>
    /// <param name="m">The m value.</param>
    /// <param name="s">The s value.</param>
    /// <param name="ms">The ms value.</param>
    /// <returns>The result of the operation.</returns>
    public JobConsumerOptions SetHeartbeatInterval(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        HeartbeatInterval = value;

        return this;
    }

    /// <summary>
    /// Sets heartbeat interval.
    /// </summary>
    /// <param name="interval">The interval value.</param>
    /// <returns>The result of the operation.</returns>
    public JobConsumerOptions SetHeartbeatInterval(TimeSpan interval)
    {
        HeartbeatInterval = interval;

        return this;
    }

    /// <summary>
    /// Sets rejected job delay.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <param name="h">The h value.</param>
    /// <param name="m">The m value.</param>
    /// <param name="s">The s value.</param>
    /// <param name="ms">The ms value.</param>
    /// <returns>The result of the operation.</returns>
    public JobConsumerOptions SetRejectedJobDelay(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        RejectedJobDelay = value;

        return this;
    }

    /// <summary>
    /// Sets rejected job delay.
    /// </summary>
    /// <param name="interval">The interval value.</param>
    /// <returns>The result of the operation.</returns>
    public JobConsumerOptions SetRejectedJobDelay(TimeSpan interval)
    {
        RejectedJobDelay = interval;

        return this;
    }

    /// <summary>
    /// Sets time provider.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    /// <returns>The result of the operation.</returns>
    public JobConsumerOptions SetTimeProvider(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        return this;
    }
}
