using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted when one activity cannot be compensated.</summary>
internal sealed class RoutingSlipActivityCompensationFailedMessage :
    RoutingSlipActivityCompensationFailed
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityCompensationFailedMessage()
    {
    }

    /// <summary>Creates an activity-compensation-failed event with detached variable and log snapshots.</summary>
    /// <param name="host">The host that attempted compensation.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The activity whose compensation failed.</param>
    /// <param name="executionId">The identifier of the activity execution being compensated.</param>
    /// <param name="timestamp">The time when compensation started.</param>
    /// <param name="duration">The duration before compensation failed.</param>
    /// <param name="exceptionInfo">The captured compensation failure.</param>
    /// <param name="variables">The routing-slip variables at failure.</param>
    /// <param name="data">The log data recorded by the original activity execution.</param>
    public RoutingSlipActivityCompensationFailedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp,
        TimeSpan duration, ExceptionInfo exceptionInfo, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        Host = host;
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        ExecutionId = executionId;
        ActivityName = activityName;
        Data = RoutingSlipMessageState.Snapshot(data);
        Variables = RoutingSlipMessageState.Snapshot(variables);
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when compensation started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the identifier of the activity execution being compensated.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the activity whose compensation failed.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the activity log data used for compensation.</summary>
    public IReadOnlyDictionary<string, object> Data { get; set; } = null!;
    /// <summary>Gets or sets the captured compensation failure.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables at failure.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the duration before compensation failed.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the host that attempted compensation.</summary>
    public HostInfo Host { get; set; } = null!;
}
