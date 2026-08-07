// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// The exchange to which the published message is sent
    /// </summary>
    public TopicHandle? Topic { get; set; }

    public BrokerTopology BuildBrokerTopology()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions, TopicSubscriptions);
    }
}
