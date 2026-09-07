using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published after an activity has been compensated successfully.</summary>
public interface RoutingSlipActivityCompensated
{
    /// <summary>The tracking number of the routing slip being compensated.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The tracking number for completion of the activity.</summary>
    Guid ExecutionId { get; }

    /// <summary>The date/time when the routing slip compensation was finished.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that completed.</summary>
    string ActivityName { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The results of the activity saved for compensation.</summary>
    IDictionary<string, object> Data { get; }

    /// <summary>
    /// The routing-slip variables after this compensation completed.
    /// </summary>
    IDictionary<string, object> Variables { get; }
}
