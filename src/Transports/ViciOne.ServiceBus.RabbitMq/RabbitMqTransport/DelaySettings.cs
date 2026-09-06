using System;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ exchange settings for delayed delivery.</summary>
public interface DelaySettings :
    EntitySettings
{
    /// <summary>Returns the send address for the settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The full delay-exchange send address.</returns>
    RabbitMqEndpointAddress GetSendAddress(Uri hostAddress);

    /// <summary>Builds the delay exchange and return binding deployed before use.</summary>
    /// <returns>The delayed-delivery broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
