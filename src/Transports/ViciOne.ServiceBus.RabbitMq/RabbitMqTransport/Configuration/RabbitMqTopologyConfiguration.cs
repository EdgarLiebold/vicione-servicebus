using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Composes RabbitMQ send, publish, consume, and message topology.</summary>
public class RabbitMqTopologyConfiguration :
    IRabbitMqTopologyConfiguration
{
    readonly IRabbitMqConsumeTopologyConfigurator _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IRabbitMqPublishTopologyConfigurator _publishTopology;
    readonly IRabbitMqSendTopologyConfigurator _sendTopology;

    /// <summary>Creates root topology with RabbitMQ routing-key and global topology conventions.</summary>
    /// <param name="messageTopology">The provider-neutral message topology.</param>
    public RabbitMqTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology;

        _sendTopology = new RabbitMqSendTopology(RabbitMqEntityNameValidator.Validator);
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));
        _sendTopology.TryAddConvention(new RoutingKeySendTopologyConvention());

        _publishTopology = new RabbitMqPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new RabbitMqConsumeTopology(messageTopology, _publishTopology);
    }

    /// <summary>Creates endpoint topology that shares send and publish topology with its parent.</summary>
    /// <param name="topologyConfiguration">The parent RabbitMQ topology.</param>
    public RabbitMqTopologyConfiguration(IRabbitMqTopologyConfiguration topologyConfiguration)
    {
        _messageTopology = topologyConfiguration.Message;
        _sendTopology = topologyConfiguration.Send;
        _publishTopology = topologyConfiguration.Publish;

        _consumeTopology = new RabbitMqConsumeTopology(topologyConfiguration.Message, topologyConfiguration.Publish);
    }

    IMessageTopologyConfigurator ITopologyConfiguration.Message => _messageTopology;
    ISendTopologyConfigurator ITopologyConfiguration.Send => _sendTopology;
    IPublishTopologyConfigurator ITopologyConfiguration.Publish => _publishTopology;
    IConsumeTopologyConfigurator ITopologyConfiguration.Consume => _consumeTopology;

    IRabbitMqPublishTopologyConfigurator IRabbitMqTopologyConfiguration.Publish => _publishTopology;
    IRabbitMqSendTopologyConfigurator IRabbitMqTopologyConfiguration.Send => _sendTopology;
    IRabbitMqConsumeTopologyConfigurator IRabbitMqTopologyConfiguration.Consume => _consumeTopology;

    /// <summary>Validates RabbitMQ send, publish, and consume topology.</summary>
    /// <returns>All validation failures from the three topology components.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
