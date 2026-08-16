namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface SubscriptionHandle :
        EntityHandle
    {
        Subscription Subscription { get; }
    }
}
