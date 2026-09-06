using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures an Amazon SQS bus, its topology, host, and default queue endpoint.</summary>
public interface IAmazonSqsBusFactoryConfigurator :
    IBusFactoryConfigurator<IAmazonSqsReceiveEndpointConfigurator>,
    IAmazonSqsQueueEndpointConfigurator
{
    /// <summary>Gets the Amazon SQS send-topology configurator.</summary>
    new IAmazonSqsSendTopologyConfigurator SendTopology { get; }

    /// <summary>Gets the Amazon SNS publish-topology configurator.</summary>
    new IAmazonSqsPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures Amazon SQS send topology for a message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configureTopology">The message send-topology callback.</param>
    void Send<T>(Action<IAmazonSqsMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures Amazon SNS publish topology for a message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configureTopology">An optional message publish-topology callback.</param>
    void Publish<T>(Action<IAmazonSqsMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures Amazon SNS publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional message publish-topology callback.</param>
    void Publish(Type messageType, Action<IAmazonSqsMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>Overrides the generated default bus endpoint queue name.</summary>
    /// <param name="value">A queue name that is distinct from every application receive endpoint.</param>
    void OverrideDefaultBusEndpointQueueName(string value);

    /// <summary>Applies immutable Amazon SQS host settings to the bus.</summary>
    /// <param name="settings">The host settings to use.</param>
    void Host(AmazonSqsHostSettings settings);
}
