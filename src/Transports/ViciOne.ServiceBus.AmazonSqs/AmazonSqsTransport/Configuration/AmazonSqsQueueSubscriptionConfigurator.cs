namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Provides queue-side settings for an Amazon SNS-to-SQS subscription.</summary>
public class AmazonSqsQueueSubscriptionConfigurator :
    AmazonSqsQueueConfigurator,
    IAmazonSqsQueueSubscriptionConfigurator
{
    /// <summary>Initializes queue-side subscription configuration.</summary>
    /// <param name="queueName">The target Amazon SQS queue name.</param>
    /// <param name="durable">Whether the queue is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the queue is deleted when its endpoint stops.</param>
    protected AmazonSqsQueueSubscriptionConfigurator(string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
    }
}
