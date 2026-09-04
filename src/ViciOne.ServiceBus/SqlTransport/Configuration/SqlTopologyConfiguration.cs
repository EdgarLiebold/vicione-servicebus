using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql topology configuration implementation.
/// </summary>
public class SqlTopologyConfiguration :
    ISqlTopologyConfiguration
{
    readonly ISqlConsumeTopologyConfigurator _consumeTopology;
    readonly IMessageTopologyConfigurator _messageTopology;
    readonly ISqlPublishTopologyConfigurator _publishTopology;
    readonly ISqlSendTopologyConfigurator _sendTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    public SqlTopologyConfiguration(IMessageTopologyConfigurator messageTopology)
    {
        _messageTopology = messageTopology;

        _sendTopology = new SqlSendTopology();
        _sendTopology.ConnectSendTopologyConfigurationObserver(new DelegateSendTopologyConfigurationObserver(GlobalTopology.Send));
        _sendTopology.TryAddConvention(new RoutingKeySendTopologyConvention());
        _sendTopology.TryAddConvention(new PartitionKeySendTopologyConvention());

        _publishTopology = new SqlPublishTopology(messageTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(new DelegatePublishTopologyConfigurationObserver(GlobalTopology.Publish));

        var observer = new PublishToSendTopologyConfigurationObserver(_sendTopology);
        _publishTopology.ConnectPublishTopologyConfigurationObserver(observer);

        _consumeTopology = new SqlConsumeTopology(_publishTopology);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public SqlTopologyConfiguration(ISqlTopologyConfiguration topologyConfiguration)
    {
        _messageTopology = topologyConfiguration.Message;
        _sendTopology = topologyConfiguration.Send;
        _publishTopology = topologyConfiguration.Publish;

        _consumeTopology = new SqlConsumeTopology(topologyConfiguration.Publish);
    }

    IMessageTopologyConfigurator ITopologyConfiguration.Message => _messageTopology;
    ISendTopologyConfigurator ITopologyConfiguration.Send => _sendTopology;
    IPublishTopologyConfigurator ITopologyConfiguration.Publish => _publishTopology;
    IConsumeTopologyConfigurator ITopologyConfiguration.Consume => _consumeTopology;

    ISqlPublishTopologyConfigurator ISqlTopologyConfiguration.Publish => _publishTopology;
    ISqlSendTopologyConfigurator ISqlTopologyConfiguration.Send => _sendTopology;
    ISqlConsumeTopologyConfigurator ISqlTopologyConfiguration.Consume => _consumeTopology;

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
