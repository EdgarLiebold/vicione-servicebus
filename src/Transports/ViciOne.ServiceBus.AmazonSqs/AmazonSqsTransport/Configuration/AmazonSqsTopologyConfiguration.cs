using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Coordinates Amazon SQS send, Amazon SNS publish, and subscription topology configuration.</summary>
public class AmazonSqsTopologyConfiguration :
    IAmazonSqsTopologyConfiguration
{
    readonly IAmazonSqsConsumeTopologyConfigurator _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IAmazonSqsPublishTopologyConfigurator _publishTopology;
    readonly IAmazonSqsSendTopologyConfigurator _sendTopology;

    /// <summary>Initializes root Amazon topology and connects global topology observers.</summary>
    /// <param name="messageTopology">The message-topology convention source.</param>
    public AmazonSqsTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology;

        _sendTopology = new AmazonSqsSendTopology(AmazonSqsEntityNameValidator.Validator);
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));

        _publishTopology = new AmazonSqsPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new AmazonSqsConsumeTopology(messageTopology, _publishTopology);
    }

    /// <summary>Initializes an endpoint topology scope that shares send and publish topology with its parent.</summary>
    /// <param name="topologyConfiguration">The parent topology configuration.</param>
    public AmazonSqsTopologyConfiguration(IAmazonSqsTopologyConfiguration topologyConfiguration)
    {
        _messageTopology = topologyConfiguration.Message;
        _sendTopology = topologyConfiguration.Send;
        _publishTopology = topologyConfiguration.Publish;

        _consumeTopology = new AmazonSqsConsumeTopology(topologyConfiguration.Message, topologyConfiguration.Publish);
    }

    IMessageTopologyConfigurator ITopologyConfiguration.Message => _messageTopology;
    ISendTopologyConfigurator ITopologyConfiguration.Send => _sendTopology;
    IPublishTopologyConfigurator ITopologyConfiguration.Publish => _publishTopology;
    IConsumeTopologyConfigurator ITopologyConfiguration.Consume => _consumeTopology;

    IAmazonSqsPublishTopologyConfigurator IAmazonSqsTopologyConfiguration.Publish => _publishTopology;
    IAmazonSqsSendTopologyConfigurator IAmazonSqsTopologyConfiguration.Send => _sendTopology;
    IAmazonSqsConsumeTopologyConfigurator IAmazonSqsTopologyConfiguration.Consume => _consumeTopology;

    /// <summary>Validates send, publish, and consume topology configuration.</summary>
    /// <returns>All detected topology validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
