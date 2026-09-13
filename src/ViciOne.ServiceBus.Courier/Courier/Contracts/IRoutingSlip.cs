using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Defines the transport contract that carries routing-slip state between activities.</summary>
[ActivityMessage]
public interface IRoutingSlip
{
    /// <summary>
    /// The unique tracking number for this routing slip, used to correlate events
    /// and activities.
    /// </summary>
    Guid TrackingNumber { get; }

    /// <summary>The time when the routing slip was created.</summary>
    DateTimeOffset CreateTimestamp { get; }

    /// <summary>The list of activities that are remaining.</summary>
    IReadOnlyList<IActivity> Itinerary { get; }

    /// <summary>The logs of activities that have already been executed.</summary>
    IReadOnlyList<IActivityLog> ActivityLogs { get; }

    /// <summary>The logs of activities that can be compensated.</summary>
    IReadOnlyList<ICompensateLog> CompensateLogs { get; }

    /// <summary>Variables that are carried with the routing slip for use by any activity.</summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>A list of exceptions that have occurred during routing slip execution.</summary>
    IReadOnlyList<IActivityException> ActivityExceptions { get; }

    /// <summary>Subscriptions to routing slip events.</summary>
    IReadOnlyList<ISubscription> Subscriptions { get; }
}
