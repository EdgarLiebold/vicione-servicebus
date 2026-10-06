namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds broker topology for an Amazon SNS publish endpoint.</summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    /// <summary>Gets the Amazon SNS topic to which the message is published.</summary>
    public TopicHandle? Topic { get; set; }

    /// <summary>Creates an array snapshot of the accumulated publish topology.</summary>
    /// <returns>The broker topology snapshot.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions);
    }
}
