using System;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ActiveMqBusTopology :
    BusTopology,
    IActiveMqBusTopology
{
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly IActiveMqTopologyConfiguration _topologyConfiguration;

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

    public SendSettings GetSendSettings(Uri address)
    {
        var endpointAddress = new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);

        return _topologyConfiguration.Send.GetSendSettings(endpointAddress);
    }

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
