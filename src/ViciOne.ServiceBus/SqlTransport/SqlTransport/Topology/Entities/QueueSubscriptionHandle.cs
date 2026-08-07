// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface QueueSubscriptionHandle :
        EntityHandle
    {
        TopicToQueueSubscription Subscription { get; }
    }
}
