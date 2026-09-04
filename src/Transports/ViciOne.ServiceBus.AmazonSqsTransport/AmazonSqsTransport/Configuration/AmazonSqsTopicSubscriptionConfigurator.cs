using ViciOne.ServiceBus.AmazonSqsTransport.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

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
