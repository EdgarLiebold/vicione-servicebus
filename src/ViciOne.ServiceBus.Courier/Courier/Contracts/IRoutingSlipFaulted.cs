using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when a routing slip faults (after compensation).</summary>
public interface IRoutingSlipFaulted
{
    /// <summary>The tracking number of the routing slip that faulted.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The date/time when the routing slip faulted.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The time from when the routing slip was created until the fault occurred.</summary>
    TimeSpan Duration { get; }

    /// <summary>The exception information from the faulting activities.</summary>
    IReadOnlyList<IActivityException> ActivityExceptions { get; }

    /// <summary>
    /// The routing-slip variables after compensation completed.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }
}
