using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted after one activity is compensated successfully.</summary>
internal sealed class RoutingSlipActivityCompensatedMessage :
    RoutingSlipActivityCompensated
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityCompensatedMessage()
    {
    }

    /// <summary>Creates an activity-compensated event with detached variable and log snapshots.</summary>
    /// <param name="host">The host that compensated the activity.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The compensated activity name.</param>
    /// <param name="executionId">The identifier of the activity execution being compensated.</param>
    /// <param name="timestamp">The time when compensation started.</param>
    /// <param name="duration">The compensation duration.</param>
    /// <param name="variables">The routing-slip variables after compensation.</param>
    /// <param name="data">The log data recorded by the original activity execution.</param>
    public RoutingSlipActivityCompensatedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);

        Host = host;
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        ExecutionId = executionId;
        ActivityName = activityName;
        Data = RoutingSlipMessageState.Snapshot(data);
        Variables = RoutingSlipMessageState.Snapshot(variables);
    }

    /// <summary>Gets or sets the identifier of the compensated activity execution.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the host that compensated the activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the activity log data used for compensation.</summary>
    public IReadOnlyDictionary<string, object> Data { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables after compensation.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the compensation duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the compensated activity name.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when compensation started.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
