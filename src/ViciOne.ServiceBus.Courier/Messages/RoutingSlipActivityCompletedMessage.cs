using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted after one activity executes successfully.</summary>
internal sealed class RoutingSlipActivityCompletedMessage :
    RoutingSlipActivityCompleted
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityCompletedMessage()
    {
    }

    /// <summary>Creates an activity-completed event with detached argument, result, and variable snapshots.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The completed activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The time when activity execution started.</param>
    /// <param name="duration">The activity execution duration.</param>
    /// <param name="variables">The routing-slip variables after execution.</param>
    /// <param name="arguments">The arguments supplied to the activity.</param>
    /// <param name="data">The result data retained for possible compensation.</param>
    public RoutingSlipActivityCompletedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> arguments,
        IReadOnlyDictionary<string, object> data)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);

        Host = host;
        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ExecutionId = executionId;
        ActivityName = activityName;
        Data = RoutingSlipMessageState.Snapshot(data);
        Variables = RoutingSlipMessageState.Snapshot(variables);
        Arguments = RoutingSlipMessageState.Snapshot(arguments);
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when activity execution started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the activity execution duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the completed activity name.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the host that executed the activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the arguments supplied to the activity.</summary>
    public IReadOnlyDictionary<string, object> Arguments { get; set; } = null!;
    /// <summary>Gets or sets the result data retained for possible compensation.</summary>
    public IReadOnlyDictionary<string, object> Data { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables after execution.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
}
