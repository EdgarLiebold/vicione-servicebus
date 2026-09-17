using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the completion record of one routing-slip activity execution.</summary>
internal sealed class RoutingSlipActivityLog :
    IActivityLog
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityLog()
    {
    }

    /// <summary>Creates an activity-completion record.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="executionId">The non-empty activity execution identifier.</param>
    /// <param name="name">The non-empty activity name.</param>
    /// <param name="timestamp">The time when activity execution started.</param>
    /// <param name="duration">The non-negative activity duration.</param>
    public RoutingSlipActivityLog(HostInfo host, Guid executionId, string name, DateTimeOffset timestamp, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);

        ExecutionId = executionId;
        Name = name;
        Timestamp = timestamp;
        Duration = duration;
        Host = host;
    }

    /// <summary>Creates a validated snapshot of a received activity-completion record.</summary>
    /// <param name="activityLog">The received activity log to copy.</param>
    public RoutingSlipActivityLog(IActivityLog activityLog)
    {
        ArgumentNullException.ThrowIfNull(activityLog);

        HostInfo? host = activityLog.Host;
        if (host is null)
            throw new SerializationException("An activity log requires host information.");

        Guid executionId = activityLog.ExecutionId;
        if (executionId == Guid.Empty)
            throw new SerializationException("An activity log requires a non-empty execution identifier.");

        string? name = activityLog.Name;
        if (string.IsNullOrWhiteSpace(name))
            throw new SerializationException("An activity log requires a name.");

        TimeSpan duration = activityLog.Duration;
        if (duration < TimeSpan.Zero)
            throw new SerializationException("An activity log cannot have a negative duration.");

        ExecutionId = executionId;
        Name = name;
        Timestamp = activityLog.Timestamp;
        Duration = duration;
        Host = host;
    }

    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the activity name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the time when activity execution started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the activity duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the host that executed the activity.</summary>
    public HostInfo Host { get; set; } = null!;
}
