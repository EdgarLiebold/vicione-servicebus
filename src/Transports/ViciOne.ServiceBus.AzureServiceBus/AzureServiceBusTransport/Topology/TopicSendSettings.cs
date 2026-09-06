using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides Azure Service Bus topic declaration and sender settings.</summary>
public class TopicSendSettings :
    SendSettings
{
    readonly BrokerTopology _brokerTopology;
    readonly CreateTopicOptions _createTopicOptions;

    /// <summary>Creates sender settings from topic options and publish topology.</summary>
    /// <param name="createTopicOptions">The Azure topic declaration options.</param>
    /// <param name="brokerTopology">The topics and forwarding relationships to declare.</param>
    public TopicSendSettings(CreateTopicOptions createTopicOptions, BrokerTopology brokerTopology)
    {
        _createTopicOptions = createTopicOptions;
        _brokerTopology = brokerTopology;
    }

    /// <summary>Gets the namespace-relative topic path.</summary>
    public string EntityPath => _createTopicOptions.Name;

    /// <summary>Gets the publish topology associated with the destination topic.</summary>
    /// <returns>The topics and forwarding relationships to declare.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        return _brokerTopology;
    }
}
