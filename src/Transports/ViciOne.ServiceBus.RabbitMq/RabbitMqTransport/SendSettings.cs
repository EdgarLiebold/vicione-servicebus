using System;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ exchange and optional queue topology for a send destination.</summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>Returns the send address for the settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The full destination address.</returns>
    RabbitMqEndpointAddress GetSendAddress(Uri hostAddress);

    /// <summary>Builds the exchange and optional queue topology deployed before use.</summary>
    /// <returns>The destination broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
