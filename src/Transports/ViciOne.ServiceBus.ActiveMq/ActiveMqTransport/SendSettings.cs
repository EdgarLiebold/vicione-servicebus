using System;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines the address and broker topology of an ActiveMQ send destination.</summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>Returns the send address for the settings.</summary>
    /// <param name="hostAddress">The configured broker address.</param>
    /// <returns>The absolute ActiveMQ destination address.</returns>
    Uri GetSendAddress(Uri hostAddress);

    /// <summary>Creates the topic and queue topology required by this send destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
