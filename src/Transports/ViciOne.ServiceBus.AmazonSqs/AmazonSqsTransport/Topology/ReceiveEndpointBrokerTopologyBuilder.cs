namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a receive endpoint broker topology builder implementation.
/// </summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    public QueueHandle? Queue { get; set; }

    /// <summary>
    /// Performs the build topology layout operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildTopologyLayout()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions, TopicSubscriptions);
    }
}
