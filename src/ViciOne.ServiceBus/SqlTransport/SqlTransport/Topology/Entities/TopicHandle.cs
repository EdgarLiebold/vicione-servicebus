namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface TopicHandle :
        EntityHandle
    {
        Topic Topic { get; }
    }
}
