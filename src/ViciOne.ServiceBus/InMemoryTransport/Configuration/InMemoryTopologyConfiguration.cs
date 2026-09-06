using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Stores and validates in memory topology configuration.</summary>
public class InMemoryTopologyConfiguration :
    IInMemoryTopologyConfiguration
{
    readonly InMemoryConsumeTopology _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IInMemoryPublishTopologyConfigurator _publishTopology;
    readonly ISendTopologyConfigurator _sendTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageTopology">The message topology.</param>
    public InMemoryTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology;

        _sendTopology = new SendTopology();
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));
        _sendTopology.TryAddConvention(new RoutingKeySendTopologyConvention());

        _publishTopology = new InMemoryPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new InMemoryConsumeTopology(messageTopology, _publishTopology);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    public InMemoryTopologyConfiguration(IInMemoryTopologyConfiguration topologyConfiguration)
    {
        _messageTopology = topologyConfiguration.Message;
        _sendTopology = topologyConfiguration.Send;
        _publishTopology = topologyConfiguration.Publish;

        _consumeTopology = new InMemoryConsumeTopology(topologyConfiguration.Message, _publishTopology);
    }

    IMessageTopologyConfigurator ITopologyConfiguration.Message => _messageTopology;
    ISendTopologyConfigurator ITopologyConfiguration.Send => _sendTopology;
    IPublishTopologyConfigurator ITopologyConfiguration.Publish => _publishTopology;
    IConsumeTopologyConfigurator ITopologyConfiguration.Consume => _consumeTopology;

    IInMemoryPublishTopologyConfigurator IInMemoryTopologyConfiguration.Publish => _publishTopology;
    IInMemoryConsumeTopologyConfigurator IInMemoryTopologyConfiguration.Consume => _consumeTopology;

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
