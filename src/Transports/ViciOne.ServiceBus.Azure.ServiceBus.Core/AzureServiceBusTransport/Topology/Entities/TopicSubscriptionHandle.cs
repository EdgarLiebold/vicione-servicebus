namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface TopicSubscriptionHandle :
        EntityHandle
    {
        TopicSubscription TopicSubscription { get; }
    }
}
