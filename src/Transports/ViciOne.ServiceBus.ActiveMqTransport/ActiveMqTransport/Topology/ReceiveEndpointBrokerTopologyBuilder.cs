namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    public QueueHandle Queue { get; set; } = null!;

    public BrokerTopology BuildTopologyLayout()
    {
        return new ActiveMqBrokerTopology(Topics, Queues, Consumers);
    }
}
