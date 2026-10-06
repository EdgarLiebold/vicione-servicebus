namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds an Amazon SQS receive queue and its Amazon SNS subscriptions.</summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>Gets or sets the consuming Amazon SQS queue handle.</summary>
    public QueueHandle? Queue { get; set; }

    /// <summary>Creates an array snapshot of the accumulated receive topology.</summary>
    /// <returns>The broker topology snapshot.</returns>
    public BrokerTopology BuildTopologyLayout()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions);
    }
}
