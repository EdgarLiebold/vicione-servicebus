// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using Topology;


public class AmazonSqsTopicSubscriptionConfigurator :
    AmazonSqsTopicConfigurator,
    IAmazonSqsTopicSubscriptionConfigurator
{
    public AmazonSqsTopicSubscriptionConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    public AmazonSqsTopicSubscriptionConfigurator(Topic topic)
        : base(topic)
    {
    }
}
