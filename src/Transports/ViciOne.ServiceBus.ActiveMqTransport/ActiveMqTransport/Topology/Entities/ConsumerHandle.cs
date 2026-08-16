namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface ConsumerHandle :
        EntityHandle
    {
        Consumer Consumer { get; }
    }
}
