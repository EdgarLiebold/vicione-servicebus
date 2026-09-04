using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs bus factory configurator.
/// </summary>
public interface IAmazonSqsBusFactoryConfigurator :
    IBusFactoryConfigurator<IAmazonSqsReceiveEndpointConfigurator>,
    IAmazonSqsQueueEndpointConfigurator
{
    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IAmazonSqsSendTopologyConfigurator SendTopology { get; }

    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IAmazonSqsPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Send<T>(Action<IAmazonSqsMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Publish<T>(Action<IAmazonSqsMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    void Publish(Type messageType, Action<IAmazonSqsMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// In most cases, this is not needed and should not be used. However, if for any reason the default bus
    /// endpoint queue name needs to be changed, this will do it. Do NOT set it to the same name as a receive
    /// endpoint or you will screw things up.
    /// </summary>
    void OverrideDefaultBusEndpointQueueName(string value);

    /// <summary>
    /// Configure a Host that can be connected. If only one host is specified, it is used as the default
    /// host for receive endpoints.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    void Host(AmazonSqsHostSettings settings);
}
