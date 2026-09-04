using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq publish topology configurator.
/// </summary>
public interface IRabbitMqPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IRabbitMqPublishTopology
{
    /// <summary>
    /// Determines how type hierarchy is configured on the broker
    /// </summary>
    new PublishBrokerTopologyOptions BrokerTopologyOptions { set; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
