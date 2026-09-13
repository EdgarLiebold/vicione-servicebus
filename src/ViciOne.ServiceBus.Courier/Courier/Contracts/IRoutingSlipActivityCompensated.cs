using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Published after an activity has been compensated successfully.</summary>
public interface IRoutingSlipActivityCompensated
{
    /// <summary>The tracking number of the routing slip being compensated.</summary>
    Guid TrackingNumber { get; }

    /// <summary>The identifier of the activity execution being compensated.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when this compensation attempt started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of this compensation attempt.</summary>
    TimeSpan Duration { get; }

    /// <summary>The name of the activity that was compensated.</summary>
    string ActivityName { get; }

    /// <summary>The host that compensated the activity.</summary>
    HostInfo Host { get; }

    /// <summary>The results of the activity saved for compensation.</summary>
    IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// The routing-slip variables after this compensation completed.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }
}
