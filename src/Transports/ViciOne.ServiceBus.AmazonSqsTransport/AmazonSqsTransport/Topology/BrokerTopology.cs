// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public interface BrokerTopology :
    IProbeSite
{
    Topic[] Topics { get; }
    Queue[] Queues { get; }
    QueueSubscription[] QueueSubscriptions { get; }
    TopicSubscription[] TopicSubscriptions { get; }
}
