namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

public class AmazonSqsQueueSubscriptionConfigurator :
    AmazonSqsQueueConfigurator,
    IAmazonSqsQueueSubscriptionConfigurator
{
    protected AmazonSqsQueueSubscriptionConfigurator(string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
    }
}
