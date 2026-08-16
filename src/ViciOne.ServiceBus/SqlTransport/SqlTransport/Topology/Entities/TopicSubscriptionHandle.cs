namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface TopicSubscriptionHandle :
        EntityHandle
    {
        TopicToTopicSubscription Subscription { get; }
    }
}
