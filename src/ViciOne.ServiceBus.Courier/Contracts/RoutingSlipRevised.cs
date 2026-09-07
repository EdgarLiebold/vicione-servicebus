using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when a routing slip is revised during execution.</summary>
public interface RoutingSlipRevised
{
    /// <summary>The tracking number of the routing slip that was revised.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The execution that modified the routing slip.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when the routing slip was revised.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The elapsed time from routing-slip creation through the revision.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that revised the routing slip.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>
    /// The routing-slip variables after the revision.
    /// </summary>
    IDictionary<string, object> Variables { get; }

    /// <summary>The new itinerary for the routing slip.</summary>
    Activity[] Itinerary { get; }

    /// <summary>The previous itinerary of the routing slip that is no longer included.</summary>
    Activity[] DiscardedItinerary { get; }
}
