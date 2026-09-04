using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

public interface TransportReceiveContext
{
    /// <summary>
    /// Gets the stable transport identity used by diagnostics. This identifies the product
    /// transport, independently of the wire protocol used by the broker connection.
    /// </summary>
    string ActivitySystem => string.Empty;

    /// <summary>
    /// Write any transport-specific properties to the dictionary so that they can be
    /// restored on subsequent outgoing messages (scheduled)
    /// </summary>
    IDictionary<string, object>? GetTransportProperties();
}
