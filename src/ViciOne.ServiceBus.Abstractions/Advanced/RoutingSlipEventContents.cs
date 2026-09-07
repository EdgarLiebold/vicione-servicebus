using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Selects the routing-slip data included in subscription events.</summary>
[Flags]
public enum RoutingSlipEventContents
{
    /// <summary>Excludes optional payload components.</summary>
    None = 0,

    /// <summary>Includes the routing-slip variables at the event boundary.</summary>
    Variables = 0x0001,

    /// <summary>Includes the activity arguments.</summary>
    Arguments = 0x0002,

    /// <summary>Includes the activity completion or compensation data.</summary>
    Data = 0x0004,

    /// <summary>Includes the retained and discarded itinerary segments for revision or termination events.</summary>
    Itinerary = 0x0008,

    /// <summary>Includes every payload component supported by the event.</summary>
    All = Variables | Arguments | Data | Itinerary
}
