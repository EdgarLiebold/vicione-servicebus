namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface ExchangeBindingHandle :
        EntityHandle
    {
        ExchangeToExchangeBinding Binding { get; }
    }
}
