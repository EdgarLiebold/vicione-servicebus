namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueHandle :
        EntityHandle
    {
        Queue Queue { get; }
    }
}
