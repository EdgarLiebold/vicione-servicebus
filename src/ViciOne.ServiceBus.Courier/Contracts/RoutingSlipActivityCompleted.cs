using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published after an activity has completed successfully.</summary>
public interface RoutingSlipActivityCompleted
{
    /// <summary>The tracking number of the routing slip containing the activity.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The tracking number for completion of the activity.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when activity execution completed.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that completed.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The arguments that were specified for the activity.</summary>
    IDictionary<string, object> Arguments { get; }

    /// <summary>The activity result saved for possible compensation.</summary>
    IDictionary<string, object> Data { get; }

    /// <summary>
    /// The routing-slip variables after the activity completed.
    /// </summary>
    IDictionary<string, object> Variables { get; }
}
