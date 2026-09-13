using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when a routing slip is revised during execution.</summary>
public interface IRoutingSlipRevised
{
    /// <summary>The tracking number of the routing slip that was revised.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The execution that modified the routing slip.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when execution of the revising activity started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the revising activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that revised the routing slip.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>
    /// The routing-slip variables after the revision.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>The new itinerary for the routing slip.</summary>
    IReadOnlyList<IActivity> Itinerary { get; }

    /// <summary>The previous itinerary of the routing slip that is no longer included.</summary>
    IReadOnlyList<IActivity> DiscardedItinerary { get; }
}
