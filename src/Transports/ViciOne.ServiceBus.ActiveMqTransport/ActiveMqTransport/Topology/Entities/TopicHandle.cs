namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface TopicHandle :
        EntityHandle
    {
        Topic Topic { get; }
    }
}
