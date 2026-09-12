using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when a routing slip is terminated.</summary>
public interface RoutingSlipTerminated
{
    /// <summary>The tracking number of the routing slip that was terminated.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The execution that terminated the routing slip.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when execution of the terminating activity started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the terminating activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that terminated the routing slip.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>
    /// The routing-slip variables at termination.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>The remainder of the itinerary that will not be executed by the routing slip engine.</summary>
    IReadOnlyList<Activity> DiscardedItinerary { get; }
}
