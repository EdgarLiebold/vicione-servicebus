using System;
using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Resolves Amazon SQS send and Amazon SNS publish topology and destination addresses for a bus.</summary>
public interface IAmazonSqsBusTopology :
    IBusTopology
{
    /// <summary>Gets the Amazon SNS publish topology.</summary>
    new IAmazonSqsPublishTopology PublishTopology { get; }

    /// <summary>Gets the Amazon SQS send topology.</summary>
    new IAmazonSqsSendTopology SendTopology { get; }

    /// <summary>Creates an Amazon SNS destination address for a named topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">The callback that configures the topic.</param>
    /// <returns>The configured topic address.</returns>
    Uri GetDestinationAddress(string topicName, Action<IAmazonSqsTopicConfigurator>? configure = null);

    /// <summary>Creates an Amazon SNS destination address for a message type.</summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="configure">The callback that configures the message topic.</param>
    /// <returns>The configured topic address.</returns>
    Uri GetDestinationAddress(Type messageType, Action<IAmazonSqsTopicConfigurator>? configure = null);

    /// <summary>Resolves queue or topic send settings from an endpoint address and its query options.</summary>
    /// <param name="address">The Amazon SQS endpoint address.</param>
    /// <returns>The send settings for the address.</returns>
    SendSettings GetSendSettings(Uri address);

    /// <summary>Gets Amazon SNS publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The typed message publish topology.</returns>
    new IAmazonSqsMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Gets Amazon SQS send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <returns>The typed message send topology.</returns>
    new IAmazonSqsMessageSendTopology<T> Send<T>()
        where T : class;
}
