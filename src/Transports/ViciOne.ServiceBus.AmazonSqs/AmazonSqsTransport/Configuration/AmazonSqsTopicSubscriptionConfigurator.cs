using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs topic subscription configurator implementation.
/// </summary>
public class AmazonSqsTopicSubscriptionConfigurator :
    AmazonSqsTopicConfigurator,
    IAmazonSqsTopicSubscriptionConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public AmazonSqsTopicSubscriptionConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    public AmazonSqsTopicSubscriptionConfigurator(Topic topic)
        : base(topic)
    {
    }
}
