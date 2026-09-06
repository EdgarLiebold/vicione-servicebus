using System;
using ViciOne.ServiceBus.ActiveMq;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Resolves ActiveMQ send and publish destinations from configured topology.</summary>
public interface IActiveMqBusTopology :
    IBusTopology
{
    /// <summary>Gets the ActiveMQ publish topology.</summary>
    new IActiveMqPublishTopology PublishTopology { get; }

    /// <summary>Gets the ActiveMQ send topology.</summary>
    new IActiveMqSendTopology SendTopology { get; }

    /// <summary>Returns the destination address for the specified topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">The callback that configures the topic.</param>
    /// <returns>The absolute topic destination address.</returns>
    Uri GetDestinationAddress(string topicName, Action<IActiveMqTopicConfigurator>? configure = null);

    /// <summary>Returns the destination address for the specified message type.</summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="configure">The callback that configures the message topic.</param>
    /// <returns>The absolute publish-topic address.</returns>
    Uri GetDestinationAddress(Type messageType, Action<IActiveMqTopicConfigurator>? configure = null);

    /// <summary>
    /// Parses a destination address and its query options into queue or topic send settings.
    /// </summary>
    /// <param name="address">The ActiveMQ endpoint address.</param>
    /// <returns>The send settings for the address.</returns>
    SendSettings GetSendSettings(Uri address);

    /// <summary>Gets publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The ActiveMQ message publish topology.</returns>
    new IActiveMqMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Gets send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <returns>The ActiveMQ message send topology.</returns>
    new IActiveMqMessageSendTopology<T> Send<T>()
        where T : class;
}
