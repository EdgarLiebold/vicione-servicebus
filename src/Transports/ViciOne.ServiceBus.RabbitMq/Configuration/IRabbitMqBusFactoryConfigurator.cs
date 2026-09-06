using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures a RabbitMQ-backed bus and its transport topology.</summary>
public interface IRabbitMqBusFactoryConfigurator :
    IBusFactoryConfigurator<IRabbitMqReceiveEndpointConfigurator>,
    IRabbitMqQueueEndpointConfigurator
{
    /// <summary>Gets the send topology.</summary>
    new IRabbitMqSendTopologyConfigurator SendTopology { get; }

    /// <summary>Gets the publish topology.</summary>
    new IRabbitMqPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configureTopology">An optional callback that customizes the message send topology.</param>
    void Send<T>(Action<IRabbitMqMessageSendTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configureTopology">An optional callback that customizes the message publish topology.</param>
    void Publish<T>(Action<IRabbitMqMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures publish topology for a runtime message-contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that customizes the message publish topology.</param>
    void Publish(Type messageType, Action<IRabbitMqMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// Overrides the queue name of the bus endpoint. The name must not collide with a receive endpoint queue.
    /// </summary>
    /// <param name="queueName">The replacement bus-endpoint queue name.</param>
    void OverrideDefaultBusEndpointQueueName(string queueName);

    /// <summary>
    /// Applies the RabbitMQ host settings used by bus and receive-endpoint connections.
    /// </summary>
    /// <param name="settings">The fully configured RabbitMQ host settings.</param>
    void Host(RabbitMqHostSettings settings);
}
