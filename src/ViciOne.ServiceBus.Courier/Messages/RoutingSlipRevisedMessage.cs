using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the lifecycle event emitted when an activity replaces the remaining itinerary.</summary>
internal sealed class RoutingSlipRevisedMessage :
    RoutingSlipRevised
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipRevisedMessage()
    {
    }

    /// <summary>Creates a revised event with detached variable, active-itinerary, and discarded-itinerary snapshots.</summary>
    /// <param name="host">The host that executed the revising activity.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="activityName">The activity that revised the itinerary.</param>
    /// <param name="executionId">The revising activity execution identifier.</param>
    /// <param name="timestamp">The time when the revising activity started.</param>
    /// <param name="duration">The revising activity execution duration.</param>
    /// <param name="variables">The routing-slip variables after revision.</param>
    /// <param name="itinerary">The active itinerary after revision.</param>
    /// <param name="discardedItinerary">The prior itinerary entries excluded by the revision.</param>
    public RoutingSlipRevisedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables,
        IEnumerable<Activity> itinerary, IEnumerable<Activity> discardedItinerary)
    {
        RoutingSlipMessageState.ValidateActivity(host, trackingNumber, activityName, executionId, duration);

        Host = host;
        ActivityName = activityName;
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;
        ExecutionId = executionId;
        Variables = RoutingSlipMessageState.Snapshot(variables);
        Itinerary = RoutingSlipMessageState.SnapshotActivities(itinerary);
        DiscardedItinerary = RoutingSlipMessageState.SnapshotActivities(discardedItinerary);
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when the revising activity started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the revising activity execution duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the activity that revised the itinerary.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the revising activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the host that executed the revising activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables after revision.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the active itinerary after revision.</summary>
    public IReadOnlyList<Activity> Itinerary { get; set; } = null!;
    /// <summary>Gets or sets the prior itinerary entries excluded by the revision.</summary>
    public IReadOnlyList<Activity> DiscardedItinerary { get; set; } = null!;
}
