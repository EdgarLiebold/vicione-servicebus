using System;
using ViciOne.ServiceBus.ActiveMq;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq bus topology.
/// </summary>
public interface IActiveMqBusTopology :
    IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IActiveMqPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IActiveMqSendTopology SendTopology { get; }

    /// <summary>
    /// Returns the destination address for the specified exchange
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(string topicName, Action<IActiveMqTopicConfigurator>? configure = null);

    /// <summary>
    /// Returns the destination address for the specified message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(Type messageType, Action<IActiveMqTopicConfigurator>? configure = null);

    /// <summary>
    /// Returns the settings for sending to the specified address. Will parse any arguments
    /// off the query string to properly configure the settings, including exchange and queue
    /// durability, etc.
    /// </summary>
    /// <param name="address">The ActiveMQ endpoint address</param>
    /// <returns>The send settings for the address</returns>
    SendSettings GetSendSettings(Uri address);

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessageSendTopology<T> Send<T>()
        where T : class;
}
