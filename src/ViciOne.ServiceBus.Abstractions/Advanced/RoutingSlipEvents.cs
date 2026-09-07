using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Identifies the routing-slip lifecycle events delivered to a subscription.</summary>
[Flags]
public enum RoutingSlipEvents
{
    /// <summary>Subscribes to every lifecycle event.</summary>
    All = 0,

    /// <summary>Subscribes to routing-slip completion.</summary>
    Completed = 0x0001,

    /// <summary>Subscribes to terminal routing-slip failure.</summary>
    Faulted = 0x0002,

    /// <summary>Subscribes to terminal compensation failure.</summary>
    CompensationFailed = 0x0004,

    /// <summary>Subscribes to explicit routing-slip termination.</summary>
    Terminated = 0x0008,

    /// <summary>Subscribes to routing-slip itinerary revision.</summary>
    Revised = 0x0010,

    /// <summary>Subscribes to successful activity execution.</summary>
    ActivityCompleted = 0x0100,

    /// <summary>Subscribes to failed activity execution.</summary>
    ActivityFaulted = 0x0200,

    /// <summary>Subscribes to successful activity compensation.</summary>
    ActivityCompensated = 0x0400,

    /// <summary>Subscribes to failed activity compensation.</summary>
    ActivityCompensationFailed = 0x0800,

    /// <summary>
    /// Delivers the subscription in addition to publishing the corresponding event.
    /// Without this flag, the presence of a matching subscription suppresses publication.
    /// </summary>
    Supplemental = 0x10000
}
