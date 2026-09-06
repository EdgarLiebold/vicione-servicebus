using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Provides topic-side settings for an Amazon SNS-to-SQS subscription.</summary>
public class AmazonSqsTopicSubscriptionConfigurator :
    AmazonSqsTopicConfigurator,
    IAmazonSqsTopicSubscriptionConfigurator
{
    /// <summary>Initializes topic-side subscription configuration.</summary>
    /// <param name="topicName">The source Amazon SNS topic name.</param>
    /// <param name="durable">Whether the topic is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the topic is deleted when its endpoint stops.</param>
    public AmazonSqsTopicSubscriptionConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>Initializes subscription configuration from a topic topology entity.</summary>
    /// <param name="topic">The source topic topology entity.</param>
    public AmazonSqsTopicSubscriptionConfigurator(Topic topic)
        : base(topic)
    {
    }
}
