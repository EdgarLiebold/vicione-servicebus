using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public class TopicSendSettings :
    SendSettings
{
    readonly BrokerTopology _brokerTopology;
    readonly CreateTopicOptions _createTopicOptions;

    public TopicSendSettings(CreateTopicOptions createTopicOptions, BrokerTopology brokerTopology)
    {
        _createTopicOptions = createTopicOptions;
        _brokerTopology = brokerTopology;
    }

    public string EntityPath => _createTopicOptions.Name;

    public BrokerTopology GetBrokerTopology()
    {
        return _brokerTopology;
    }
}
