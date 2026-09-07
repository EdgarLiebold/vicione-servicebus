using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes a failure record for one routing-slip activity execution.</summary>
internal sealed class RoutingSlipActivityException :
    ActivityException
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityException()
    {
    }

    /// <summary>Creates an activity-failure record.</summary>
    /// <param name="activityName">The non-empty activity name.</param>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="executionId">The non-empty activity execution identifier.</param>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="elapsed">The non-negative duration before failure.</param>
    /// <param name="exceptionInfo">The captured activity failure.</param>
    public RoutingSlipActivityException(string activityName, HostInfo host, Guid executionId, DateTimeOffset timestamp, TimeSpan elapsed,
        ExceptionInfo exceptionInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentNullException.ThrowIfNull(host);
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        ExecutionId = executionId;

        Timestamp = timestamp;
        Elapsed = elapsed;
        Name = activityName;
        Host = host;
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Creates a validated snapshot of a received activity-failure record.</summary>
    /// <param name="activityException">The received activity failure to copy.</param>
    public RoutingSlipActivityException(ActivityException activityException)
    {
        ArgumentNullException.ThrowIfNull(activityException);

        if (string.IsNullOrWhiteSpace(activityException.Name))
            throw new SerializationException("An activity failure requires an activity name.");
        if (activityException.Host is null)
            throw new SerializationException("An activity failure requires host information.");
        if (activityException.ExecutionId == Guid.Empty)
            throw new SerializationException("An activity failure requires a non-empty execution identifier.");
        if (activityException.Elapsed < TimeSpan.Zero)
            throw new SerializationException("An activity failure cannot have a negative elapsed duration.");
        if (activityException.ExceptionInfo == null)
            throw new SerializationException("An activity failure requires exception information.");

        ExecutionId = activityException.ExecutionId;
        Timestamp = activityException.Timestamp;
        Elapsed = activityException.Elapsed;
        Name = activityException.Name;
        Host = activityException.Host;
        ExceptionInfo = activityException.ExceptionInfo;
    }

    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the failure timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration before failure.</summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>Gets or sets the activity name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the host that executed the activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the captured activity failure.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
}
