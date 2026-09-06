using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Exposes state for transport receive operations.</summary>
public interface TransportReceiveContext
{
    /// <summary>Gets the activity system.</summary>
    string ActivitySystem => string.Empty;

    /// <summary>
    /// Write any transport-specific properties to the dictionary so that they can be
    /// restored on subsequent outgoing messages (scheduled).
    /// </summary>
    /// <returns>The transport properties.</returns>
    IDictionary<string, object>? GetTransportProperties();
}
