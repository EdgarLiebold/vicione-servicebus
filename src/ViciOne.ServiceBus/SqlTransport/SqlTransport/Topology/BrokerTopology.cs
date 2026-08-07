// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    public interface BrokerTopology :
        IProbeSite
    {
        Topic[] Topics { get; }
        Queue[] Queues { get; }
        TopicToTopicSubscription[] TopicSubscriptions { get; }
        TopicToQueueSubscription[] QueueSubscriptions { get; }
    }
}
