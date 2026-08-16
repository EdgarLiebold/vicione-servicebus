namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface TopicHandle :
        EntityHandle
    {
        Topic Topic { get; }
    }
}
