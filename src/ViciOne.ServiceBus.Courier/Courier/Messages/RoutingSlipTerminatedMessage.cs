using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted when an activity terminates a routing slip successfully.</summary>
internal sealed class RoutingSlipTerminatedMessage :
    IRoutingSlipTerminated
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipTerminatedMessage()
    {
    }

    /// <summary>Creates a terminated event with detached variable and discarded-itinerary snapshots.</summary>
    /// <param name="host">The host that executed the terminating activity.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The activity that terminated the routing slip.</param>
    /// <param name="executionId">The terminating activity execution identifier.</param>
    /// <param name="timestamp">The time when the terminating activity started.</param>
    /// <param name="duration">The terminating activity execution duration.</param>
    /// <param name="variables">The routing-slip variables at termination.</param>
    /// <param name="discardedItinerary">The remaining itinerary that will not execute.</param>
    public RoutingSlipTerminatedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables, IEnumerable<IActivity> discardedItinerary)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);

        Host = host;
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        ActivityName = activityName;
        Variables = RoutingSlipMessageState.Snapshot(variables);
        DiscardedItinerary = RoutingSlipMessageState.SnapshotActivities(discardedItinerary);
        ExecutionId = executionId;
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when the terminating activity started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the terminating activity execution duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets the activity that terminated the routing slip.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the terminating activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the host that executed the terminating activity.</summary>
    public HostInfo Host { get; set; } = null!;

    /// <summary>Gets or sets the routing-slip variables at termination.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the remaining itinerary that will not execute.</summary>
    public IReadOnlyList<IActivity> DiscardedItinerary { get; set; } = null!;
}
