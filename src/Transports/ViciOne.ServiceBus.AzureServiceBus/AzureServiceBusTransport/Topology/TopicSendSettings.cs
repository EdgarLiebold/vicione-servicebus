using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a topic send settings implementation.
/// </summary>
public class TopicSendSettings :
    SendSettings
{
    readonly BrokerTopology _brokerTopology;
    readonly CreateTopicOptions _createTopicOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="createTopicOptions">The create topic options value.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    public TopicSendSettings(CreateTopicOptions createTopicOptions, BrokerTopology brokerTopology)
    {
        _createTopicOptions = createTopicOptions;
        _brokerTopology = brokerTopology;
    }

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _createTopicOptions.Name;

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        return _brokerTopology;
    }
}
