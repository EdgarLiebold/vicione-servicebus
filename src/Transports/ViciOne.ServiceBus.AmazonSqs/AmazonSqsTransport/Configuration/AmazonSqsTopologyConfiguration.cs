using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs topology configuration implementation.
/// </summary>
public class AmazonSqsTopologyConfiguration :
    IAmazonSqsTopologyConfiguration
{
    readonly IAmazonSqsConsumeTopologyConfigurator _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IAmazonSqsPublishTopologyConfigurator _publishTopology;
    readonly IAmazonSqsSendTopologyConfigurator _sendTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
