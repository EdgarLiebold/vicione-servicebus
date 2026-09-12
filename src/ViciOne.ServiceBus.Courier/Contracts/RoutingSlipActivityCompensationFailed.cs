using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when an activity cannot be compensated.</summary>
public interface RoutingSlipActivityCompensationFailed
{
    /// <summary>The tracking number of the routing slip being compensated.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The identifier of the activity execution whose compensation failed.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when this compensation attempt started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration before this compensation attempt failed.</summary>
    TimeSpan Duration { get; }

    /// <summary>The host that attempted to compensate the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The name of the activity that failed to compensate.</summary>
    string ActivityName { get; }

    /// <summary>The results of the activity saved for compensation.</summary>
    IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// The routing-slip variables at the point compensation failed.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>The exception information from the faulting compensation.</summary>
    ExceptionInfo ExceptionInfo { get; }
}
