using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines settings for send.</summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>Returns the send address for the settings.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The send address.</returns>
    SqlEndpointAddress GetSendAddress(Uri hostAddress);

    /// <summary>Creates the topic, queue, and subscription topology required by this send destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
