using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq bus topology implementation.
/// </summary>
public class ActiveMqBusTopology :
    BusTopology,
    IActiveMqBusTopology
{
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly IActiveMqTopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public ActiveMqBusTopology(IActiveMqHostConfiguration hostConfiguration, IActiveMqTopologyConfiguration topologyConfiguration)
        : base(hostConfiguration, topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    IActiveMqPublishTopology IActiveMqBusTopology.PublishTopology => _topologyConfiguration.Publish;
    IActiveMqSendTopology IActiveMqBusTopology.SendTopology => _topologyConfiguration.Send;

    IActiveMqMessagePublishTopology<T> IActiveMqBusTopology.Publish<T>()
    {
        return _topologyConfiguration.Publish.GetMessageTopology<T>();
    }

    IActiveMqMessageSendTopology<T> IActiveMqBusTopology.Send<T>()
    {
        return _topologyConfiguration.Send.GetMessageTopology<T>();
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(Uri address)
    {
        var endpointAddress = new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);

        return _topologyConfiguration.Send.GetSendSettings(endpointAddress);
    }

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetDestinationAddress(string topicName, Action<IActiveMqTopicConfigurator>? configure = null)
    {
        var address = new ActiveMqEndpointAddress(
            _hostConfiguration.HostAddress,
            topicName,
            type: ActiveMqEndpointAddress.AddressType.Topic);

        var sendSettings = new ActiveMqTopicSendSettings(address);

        configure?.Invoke(sendSettings);

        return sendSettings.GetSendAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetDestinationAddress(Type messageType, Action<IActiveMqTopicConfigurator>? configure = null)
    {
        var isTemporary = MessageTypeCache.IsTemporaryMessageType(messageType);

        if (!_topologyConfiguration.Publish.TryGetPublishAddress(messageType, _hostConfiguration.HostAddress, out var address)
            || address == null)
            throw new ArgumentException($"No ActiveMQ publish topology is configured for {messageType.FullName}.", nameof(messageType));

        var settings = new ActiveMqTopicSendSettings(new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address));
        if (isTemporary)
        {
            settings.AutoDelete = true;
            settings.Durable = false;
        }

        configure?.Invoke(settings);

        return settings.GetSendAddress(_hostConfiguration.HostAddress);
    }
}
