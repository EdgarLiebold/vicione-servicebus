using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq bus factory configurator.
/// </summary>
public interface IActiveMqBusFactoryConfigurator :
    IBusFactoryConfigurator<IActiveMqReceiveEndpointConfigurator>,
    IActiveMqQueueEndpointConfigurator
{
    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    new IActiveMqSendTopologyConfigurator SendTopology { get; }

    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    new IActiveMqPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Send<T>(Action<IActiveMqMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Publish<T>(Action<IActiveMqMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    void Publish(Type messageType, Action<IActiveMqMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// Configure a Host that can be connected. If only one host is specified, it is used as the default
    /// host for receive endpoints.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    void Host(ActiveMqHostSettings settings);

    /// <summary>
    /// Configure the consumer topology so that it is compatible for the format required for ActiveMQ Artemis
    /// </summary>
    void EnableArtemisCompatibility();

    /// <summary>
    /// Specify the prefix to be added to temporary queue names, including the bus endpoint
    /// </summary>
    /// <param name="prefix"></param>
    void SetTemporaryQueueNamePrefix(string prefix);

    /// <summary>
    /// Specify a consumer endpoint queue name formatter. Generate name for consumer queue using
    /// topic and endpoint name
    /// </summary>
    /// <param name="formatter"></param>
    public void SetConsumerEndpointQueueNameFormatter(IActiveMqConsumerEndpointQueueNameFormatter formatter);

    /// <summary>
    /// Specify a temporary queue name formatter. Allows for the transformation of vicione-servicebus generated temporary queue names
    /// e.g. adding a prefix
    /// </summary>
    /// <param name="formatter"></param>
    public void SetTemporaryQueueNameFormatter(IActiveMqTemporaryQueueNameFormatter? formatter);
}
