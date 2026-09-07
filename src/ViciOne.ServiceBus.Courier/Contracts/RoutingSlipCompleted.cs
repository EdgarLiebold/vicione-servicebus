using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when a routing slip completes.</summary>
public interface RoutingSlipCompleted
{
    /// <summary>The tracking number of the routing slip that completed.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The date/time when the routing slip completed.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The time from when the routing slip was created until the completion.</summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// The final routing-slip variables.
    /// </summary>
    IDictionary<string, object> Variables { get; }
}
