using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when routing-slip compensation terminates with a failure.</summary>
public interface RoutingSlipCompensationFailed
{
    /// <summary>The tracking number of the routing slip that faulted.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The date/time when the routing slip compensation was finished.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The exception information from the failed compensation.</summary>
    ExceptionInfo ExceptionInfo { get; }

    /// <summary>
    /// The routing-slip variables at the point compensation failed.
    /// </summary>
    IDictionary<string, object> Variables { get; }
}
