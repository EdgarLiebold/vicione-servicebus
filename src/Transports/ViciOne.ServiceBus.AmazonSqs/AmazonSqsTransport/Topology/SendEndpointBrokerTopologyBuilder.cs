namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds broker topology for an Amazon SQS queue send endpoint.</summary>
public class SendEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISendEndpointBrokerTopologyBuilder
{
    /// <summary>The queue to which messages are sent.</summary>
    public QueueHandle? Queue { get; set; }

    /// <summary>Creates an array snapshot of the accumulated send topology.</summary>
    /// <returns>The broker topology snapshot.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new AmazonSqsBrokerTopology(Topics, Queues, QueueSubscriptions);
    }
}
