namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a send endpoint broker topology builder implementation.
/// </summary>
public class SendEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISendEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// The queue to which messages are sent
    /// </summary>
    public QueueHandle? Queue { get; set; }

    /// <summary>
    /// Performs the build broker topology operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions, TopicSubscriptions);
    }
}
