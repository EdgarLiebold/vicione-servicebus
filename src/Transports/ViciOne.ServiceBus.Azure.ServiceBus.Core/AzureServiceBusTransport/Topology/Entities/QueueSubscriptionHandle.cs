namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueSubscriptionHandle :
        EntityHandle
    {
        QueueSubscription QueueSubscription { get; }
    }
}
