using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ publish topology across message contracts.</summary>
public interface IRabbitMqPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IRabbitMqPublishTopology
{
    /// <summary>Determines how type hierarchy is configured on the broker.</summary>
    new PublishBrokerTopologyOptions BrokerTopologyOptions { set; }

    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The RabbitMQ message publish topology.</returns>
    new IRabbitMqMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets publish topology for a runtime message-contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The RabbitMQ message publish topology.</returns>
    new IRabbitMqMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
