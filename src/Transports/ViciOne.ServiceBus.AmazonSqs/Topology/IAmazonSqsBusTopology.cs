using System;
using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs bus topology.
/// </summary>
public interface IAmazonSqsBusTopology :
    IBusTopology
{
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IAmazonSqsPublishTopology PublishTopology { get; }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IAmazonSqsSendTopology SendTopology { get; }

    /// <summary>
    /// Returns the destination address for the specified topic
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(string topicName, Action<IAmazonSqsTopicConfigurator>? configure = null);

    /// <summary>
    /// Returns the destination address for the topic identified by the message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="configure">Callback to configure exchange settings</param>
    /// <returns></returns>
    Uri GetDestinationAddress(Type messageType, Action<IAmazonSqsTopicConfigurator>? configure = null);

    /// <summary>
    /// Returns the settings for sending to the specified address. Will parse any arguments
    /// off the query string to properly configure the settings, including exchange and queue
    /// durability, etc.
    /// </summary>
    /// <param name="address">The AmazonSQS endpoint address</param>
    /// <returns>The send settings for the address</returns>
    SendSettings GetSendSettings(Uri address);

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessageSendTopology<T> Send<T>()
        where T : class;
}
