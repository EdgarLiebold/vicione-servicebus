using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Describes one pending activity in a routing-slip itinerary.</summary>
public interface Activity
{
    /// <summary>Gets the activity name used for execution and event correlation.</summary>
    string Name { get; }

    /// <summary>Gets the endpoint that executes the activity.</summary>
    Uri Address { get; }

    /// <summary>Gets the activity arguments carried by the routing slip.</summary>
    IDictionary<string, object> Arguments { get; }
}
