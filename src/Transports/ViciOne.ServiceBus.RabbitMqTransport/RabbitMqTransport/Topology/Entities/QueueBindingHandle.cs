namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueBindingHandle :
        EntityHandle
    {
        ExchangeToQueueBinding Binding { get; }
    }
}
