using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ bus, its endpoints, and transport topology.</summary>
public interface IActiveMqBusFactoryConfigurator :
    IBusFactoryConfigurator<IActiveMqReceiveEndpointConfigurator>,
    IActiveMqQueueEndpointConfigurator
{
    /// <summary>Gets the ActiveMQ send-topology configurator.</summary>
    new IActiveMqSendTopologyConfigurator SendTopology { get; }

    /// <summary>Gets the ActiveMQ publish-topology configurator.</summary>
    new IActiveMqPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <param name="configureTopology">The message send-topology callback.</param>
    void Send<T>(Action<IActiveMqMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="configureTopology">An optional message publish-topology callback.</param>
    void Publish<T>(Action<IActiveMqMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures the message's publish topology.</param>
    void Publish(Type messageType, Action<IActiveMqMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// Configures the ActiveMQ host used by the bus and its receive endpoints.
    /// </summary>
    /// <param name="settings">The complete broker connection settings.</param>
    void Host(ActiveMqHostSettings settings);

    /// <summary>Enables ActiveMQ Artemis naming and AMQP delivery-delay behavior.</summary>
    void EnableArtemisCompatibility();

    /// <summary>Sets a prefix for generated temporary queue names, including the bus endpoint.</summary>
    /// <param name="prefix">The prefix, or a blank value to restore generated names.</param>
    void SetTemporaryQueueNamePrefix(string prefix);

    /// <summary>
    /// Sets the formatter for virtual-topic consumer queue or subscription names.
    /// </summary>
    /// <param name="formatter">The consumer name formatter.</param>
    public void SetConsumerEndpointQueueNameFormatter(IActiveMqConsumerEndpointQueueNameFormatter formatter);

    /// <summary>
    /// Sets a formatter that transforms generated temporary queue names.
    /// </summary>
    /// <param name="formatter">The formatter, or <see langword="null" /> to restore generated names.</param>
    public void SetTemporaryQueueNameFormatter(IActiveMqTemporaryQueueNameFormatter? formatter);
}
