namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueHandle :
        EntityHandle
    {
        Queue Queue { get; }
    }
}
