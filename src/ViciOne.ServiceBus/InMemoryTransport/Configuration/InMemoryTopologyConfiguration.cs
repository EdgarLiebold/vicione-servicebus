using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Composes message, send, publish, and consume topology for one in-memory endpoint scope.</summary>
internal sealed class InMemoryTopologyConfiguration :
    IInMemoryTopologyConfiguration
{
    readonly InMemoryConsumeTopology _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly IInMemoryPublishTopologyConfigurator _publishTopology;
    readonly ISendTopologyConfigurator _sendTopology;

    /// <summary>Creates root topology over a mutable message-topology registry.</summary>
    /// <param name="messageTopology">The message topology shared by send, publish, and consume conventions.</param>
    public InMemoryTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology ?? throw new ArgumentNullException(nameof(messageTopology));

        _sendTopology = new SendTopology();
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));
        _sendTopology.TryAddConvention(new RoutingKeySendTopologyConvention());

        _publishTopology = new InMemoryPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new InMemoryConsumeTopology(messageTopology, _publishTopology);
    }

    /// <summary>Creates endpoint topology that shares message, send, and publish state with a parent.</summary>
    /// <param name="topologyConfiguration">The parent topology configuration.</param>
    public InMemoryTopologyConfiguration(IInMemoryTopologyConfiguration topologyConfiguration)
    {
        ArgumentNullException.ThrowIfNull(topologyConfiguration);
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

    /// <summary>Validates send, publish, and consume topology in dependency order.</summary>
    /// <returns>All topology validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _sendTopology.Validate()
            .Concat(_publishTopology.Validate())
            .Concat(_consumeTopology.Validate());
    }
}
