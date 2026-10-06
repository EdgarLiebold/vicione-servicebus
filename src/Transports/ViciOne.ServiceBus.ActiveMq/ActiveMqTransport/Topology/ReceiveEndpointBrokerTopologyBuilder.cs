namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Builds the ActiveMQ queue, topics, and consumer bindings for a receive endpoint.</summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>Gets or sets the endpoint's consuming queue.</summary>
    public QueueHandle Queue { get; set; } = null!;

    /// <summary>Creates array snapshots of the accumulated receive topology with retained entity references.</summary>
    /// <returns>The configured ActiveMQ broker topology.</returns>
    public BrokerTopology BuildTopologyLayout()
    {
        return new ActiveMqBrokerTopology(Topics, Queues, Consumers);
    }
}
