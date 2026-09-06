using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries the activity log for routing slip activity.</summary>
public class RoutingSlipActivityLog :
    ActivityLog
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipActivityLog()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="name">The name.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    public RoutingSlipActivityLog(HostInfo host, Guid executionId, string name, DateTimeOffset timestamp, TimeSpan duration)
    {
        ExecutionId = executionId;
        Name = name;
        Timestamp = timestamp;
        Duration = duration;
        Host = host;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityLog">The activity log.</param>
    public RoutingSlipActivityLog(ActivityLog activityLog)
    {
        if (string.IsNullOrEmpty(activityLog.Name))
            throw new SerializationException("An ActivityLog Name is required");

        ExecutionId = activityLog.ExecutionId;
        Name = activityLog.Name;
        Timestamp = activityLog.Timestamp;
        Duration = activityLog.Duration;
        Host = activityLog.Host;
    }

    /// <summary>Gets or sets the execution id.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
}
