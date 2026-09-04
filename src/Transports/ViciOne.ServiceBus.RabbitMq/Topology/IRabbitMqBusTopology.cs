using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq bus topology.
/// </summary>
public interface IRabbitMqBusTopology :
    IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IRabbitMqPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IRabbitMqSendTopology SendTopology { get; }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessageSendTopology<T> Send<T>()
        where T : class;

    /// <summary>
    /// Returns the destination address for the specified exchange
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(string exchangeName, Action<IRabbitMqExchangeConfigurator>? configure = null);

    /// <summary>
    /// Returns the destination address for the specified message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(Type messageType, Action<IRabbitMqExchangeConfigurator>? configure = null);
}
