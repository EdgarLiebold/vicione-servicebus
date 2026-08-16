namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueSubscriptionHandle :
        EntityHandle
    {
        TopicToQueueSubscription Subscription { get; }
    }
}
