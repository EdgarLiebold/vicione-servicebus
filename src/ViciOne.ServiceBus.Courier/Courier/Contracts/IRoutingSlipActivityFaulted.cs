using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published when an activity execution faults.</summary>
public interface IRoutingSlipActivityFaulted
{
    /// <summary>The tracking number of the routing slip that faulted.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The identifier of this activity execution.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when activity execution started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that faulted.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The exception information from the faulting activity.</summary>
    ExceptionInfo ExceptionInfo { get; }

    /// <summary>The arguments that were specified for the activity at execution.</summary>
    IReadOnlyDictionary<string, object> Arguments { get; }

    /// <summary>
    /// The routing-slip variables at the point the activity faulted.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }
}
