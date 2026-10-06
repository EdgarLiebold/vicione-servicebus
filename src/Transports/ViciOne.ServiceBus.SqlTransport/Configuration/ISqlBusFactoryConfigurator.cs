using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql bus factory.</summary>
public interface ISqlBusFactoryConfigurator :
    IBusFactoryConfigurator<ISqlReceiveEndpointConfigurator>,
    ISqlQueueEndpointConfigurator
{
    /// <summary>Gets the send topology.</summary>
    new ISqlSendTopologyConfigurator SendTopology { get; }

    /// <summary>Gets the publish topology.</summary>
    new ISqlPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configure the send topology of the message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    void Send<T>(Action<ISqlMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures SQL publish topology for the message contract.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    void Publish<T>(Action<ISqlMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures SQL publish topology for the supplied message contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void Publish(Type messageType, Action<ISqlMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// In most cases, this is not needed and should not be used. However, if for any reason the default bus
    /// endpoint queue name needs to be changed, this will do it. Do NOT set it to the same name as a receive
    /// endpoint or you will screw things up.
    /// </summary>
    /// <param name="value">The value to process.</param>
    void OverrideDefaultBusEndpointQueueName(string value);

    /// <summary>
    /// Configure a Host that can be connected. If only one host is specified, it is used as the default
    /// host for receive endpoints.
    /// </summary>
    /// <param name="settings">The settings that control the operation.</param>
    void Host(SqlHostSettings settings);
}
