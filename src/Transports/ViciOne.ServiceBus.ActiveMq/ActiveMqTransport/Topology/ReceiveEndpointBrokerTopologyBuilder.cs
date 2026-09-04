namespace ViciOne.ServiceBus.ActiveMq.Topology;

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
    public QueueHandle Queue { get; set; } = null!;

    /// <summary>
    /// Performs the build topology layout operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildTopologyLayout()
    {
        return new ActiveMqBrokerTopology(Topics, Queues, Consumers);
    }
}
