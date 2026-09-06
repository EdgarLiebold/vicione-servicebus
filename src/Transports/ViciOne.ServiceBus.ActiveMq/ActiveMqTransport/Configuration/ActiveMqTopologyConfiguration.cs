using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Coordinates ActiveMQ send, publish, and consume topology configuration.</summary>
public class ActiveMqTopologyConfiguration :
    IActiveMqTopologyConfiguration
{
    readonly IActiveMqConsumeTopologyConfigurator _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IActiveMqPublishTopologyConfigurator _publishTopology;
    readonly IActiveMqSendTopologyConfigurator _sendTopology;

    /// <summary>Creates a root ActiveMQ topology configuration and connects global topology observers.</summary>
    /// <param name="messageTopology">The message-topology configurator used for publish entities.</param>
    public ActiveMqTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology;

        _sendTopology = new ActiveMqSendTopology();
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));

        _publishTopology = new ActiveMqPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new ActiveMqConsumeTopology(_publishTopology);
    }

    /// <summary>Creates endpoint topology that shares send and publish configuration and copies consume configuration.</summary>
    /// <param name="topologyConfiguration">The parent ActiveMQ topology configuration.</param>
    public ActiveMqTopologyConfiguration(IActiveMqTopologyConfiguration topologyConfiguration)
    {
        _messageTopology = topologyConfiguration.Message;
        _sendTopology = topologyConfiguration.Send;
        _publishTopology = topologyConfiguration.Publish;

        _consumeTopology = new ActiveMqConsumeTopology(topologyConfiguration.Publish, topologyConfiguration.Consume);
    }

    IMessageTopologyConfigurator ITopologyConfiguration.Message => _messageTopology;
    ISendTopologyConfigurator ITopologyConfiguration.Send => _sendTopology;
    IPublishTopologyConfigurator ITopologyConfiguration.Publish => _publishTopology;
    IConsumeTopologyConfigurator ITopologyConfiguration.Consume => _consumeTopology;

    IActiveMqPublishTopologyConfigurator IActiveMqTopologyConfiguration.Publish => _publishTopology;
    IActiveMqSendTopologyConfigurator IActiveMqTopologyConfiguration.Send => _sendTopology;
    IActiveMqConsumeTopologyConfigurator IActiveMqTopologyConfiguration.Consume => _consumeTopology;

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
