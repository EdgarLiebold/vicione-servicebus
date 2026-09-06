using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Resolves ActiveMQ send and publish destinations from configured topology.</summary>
public class ActiveMqBusTopology :
    BusTopology,
    IActiveMqBusTopology
{
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly IActiveMqTopologyConfiguration _topologyConfiguration;

    /// <summary>Creates a bus-topology resolver for an ActiveMQ host.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    /// <param name="topologyConfiguration">The ActiveMQ topology configuration.</param>
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

    /// <summary>Gets send settings for a queue or topic address.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>The ActiveMQ send settings.</returns>
    public SendSettings GetSendSettings(Uri address)
    {
        var endpointAddress = new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);

        return _topologyConfiguration.Send.GetSendSettings(endpointAddress);
    }

    /// <summary>Builds a topic destination address from an explicit topic name.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that configures topic lifecycle settings.</param>
    /// <returns>The absolute topic destination address.</returns>
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

    /// <summary>Builds the configured publish destination address for a message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures topic lifecycle settings.</param>
    /// <returns>The absolute publish-topic address.</returns>
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
