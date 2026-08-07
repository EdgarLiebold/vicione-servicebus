// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Context
{
    using System.Collections.Generic;


    public interface TransportReceiveContext
    {
        /// <summary>
        /// Write any transport-specific properties to the dictionary so that they can be
        /// restored on subsequent outgoing messages (scheduled)
        /// </summary>
        IDictionary<string, object> GetTransportProperties();
    }
}
