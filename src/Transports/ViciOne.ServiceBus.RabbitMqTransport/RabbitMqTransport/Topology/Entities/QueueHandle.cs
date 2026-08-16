namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueHandle :
        EntityHandle
    {
        Queue Queue { get; }
    }
}
