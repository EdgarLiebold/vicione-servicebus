namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a publish endpoint broker topology builder implementation.
/// </summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// The exchange to which the published message is sent
    /// </summary>
    public TopicHandle? Topic { get; set; }

    /// <summary>
    /// Performs the build broker topology operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions, TopicSubscriptions);
    }
}
