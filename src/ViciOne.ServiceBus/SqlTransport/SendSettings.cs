using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for send settings.
/// </summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>
    /// Returns the send address for the settings
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <returns></returns>
    SqlEndpointAddress GetSendAddress(Uri hostAddress);

    /// <summary>
    /// Return the BrokerTopology to apply at startup (to create exchange and queue if binding is specified)
    /// </summary>
    /// <returns></returns>
    BrokerTopology GetBrokerTopology();
}
