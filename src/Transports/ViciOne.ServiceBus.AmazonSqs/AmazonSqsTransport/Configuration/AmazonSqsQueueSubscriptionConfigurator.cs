namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs queue subscription configurator implementation.
/// </summary>
public class AmazonSqsQueueSubscriptionConfigurator :
    AmazonSqsQueueConfigurator,
    IAmazonSqsQueueSubscriptionConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected AmazonSqsQueueSubscriptionConfigurator(string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
    }
}
