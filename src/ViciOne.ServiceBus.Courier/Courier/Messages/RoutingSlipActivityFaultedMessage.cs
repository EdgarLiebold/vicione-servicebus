using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted when one activity execution faults.</summary>
internal sealed class RoutingSlipActivityFaultedMessage :
    IRoutingSlipActivityFaulted
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityFaultedMessage()
    {
    }

    /// <summary>Creates an activity-faulted event with detached argument and variable snapshots.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The faulted activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The time when activity execution started.</param>
    /// <param name="duration">The duration before execution faulted.</param>
    /// <param name="exceptionInfo">The captured activity failure.</param>
    /// <param name="variables">The routing-slip variables at failure.</param>
    /// <param name="arguments">The arguments supplied to the activity.</param>
    public RoutingSlipActivityFaultedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo, IReadOnlyDictionary<string, object> variables,
        IReadOnlyDictionary<string, object> arguments)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        Host = host;
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;
        ExecutionId = executionId;
        ActivityName = activityName;
        Variables = RoutingSlipMessageState.Snapshot(variables);
        Arguments = RoutingSlipMessageState.Snapshot(arguments);
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when activity execution started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration before execution faulted.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the faulted activity name.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the host that executed the activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the captured activity failure.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>Gets or sets the arguments supplied to the activity.</summary>
    public IReadOnlyDictionary<string, object> Arguments { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables at failure.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
}
