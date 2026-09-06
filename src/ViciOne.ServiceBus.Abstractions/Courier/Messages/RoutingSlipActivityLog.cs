using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip activity log implementation.
/// </summary>
public class RoutingSlipActivityLog :
    ActivityLog
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipActivityLog()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    public RoutingSlipActivityLog(HostInfo host, Guid executionId, string name, DateTimeOffset timestamp, TimeSpan duration)
    {
        ExecutionId = executionId;
        Name = name;
        Timestamp = timestamp;
        Duration = duration;
        Host = host;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityLog">The activity log value.</param>
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

    /// <summary>
    /// Gets or sets the execution id value.
    /// </summary>
    public Guid ExecutionId { get; set; }
    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
}
