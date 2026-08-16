namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface ExchangeHandle :
        EntityHandle
    {
        Exchange Exchange { get; }
    }
}
