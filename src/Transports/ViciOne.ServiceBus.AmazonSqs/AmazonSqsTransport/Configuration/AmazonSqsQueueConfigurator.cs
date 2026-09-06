using System.Collections.Generic;
using Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Configures an Amazon SQS queue and subscriptions targeting it.</summary>
public class AmazonSqsQueueConfigurator :
    EntityConfigurator,
    IAmazonSqsQueueConfigurator,
    Queue
{
    /// <summary>Initializes queue configuration and marks <c>.fifo</c> queues as FIFO entities.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="durable">Whether the queue is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the queue is deleted when its endpoint stops.</param>
    /// <param name="queueAttributes">Optional Amazon SQS queue attributes.</param>
    /// <param name="queueSubscriptionAttributes">Optional Amazon SNS attributes for subscriptions targeting the queue.</param>
    /// <param name="queueTags">Optional tags applied to the queue.</param>
    protected AmazonSqsQueueConfigurator(string queueName, bool durable = true, bool autoDelete = false,
        IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? queueTags = null)
        : base(queueName, durable, autoDelete)
    {
        QueueAttributes = queueAttributes ?? new Dictionary<string, object>();
        QueueSubscriptionAttributes = queueSubscriptionAttributes ?? new Dictionary<string, object>();
        QueueTags = queueTags ?? new Dictionary<string, string>();

        if (AmazonSqsEndpointAddress.IsFifo(queueName))
            QueueAttributes[QueueAttributeName.FifoQueue] = "true";
    }

    /// <summary>Initializes queue configuration from an existing topology entity.</summary>
    /// <param name="source">The queue topology entity whose settings and collections are reused.</param>
    public AmazonSqsQueueConfigurator(Queue source)
        : base(source.EntityName, source.Durable, source.AutoDelete)
    {
        QueueAttributes = source.QueueAttributes;
        QueueSubscriptionAttributes = source.QueueSubscriptionAttributes;
        QueueTags = source.QueueTags;
    }

    /// <summary>Gets the tags applied to the queue.</summary>
    public IDictionary<string, string> Tags => QueueTags;

    /// <summary>Gets the queue address type used for endpoint-address formatting.</summary>
    protected override AmazonSqsEndpointAddress.AddressType AddressType => AmazonSqsEndpointAddress.AddressType.Queue;

    /// <summary>Gets the Amazon SQS queue attributes.</summary>
    public IDictionary<string, object> QueueAttributes { get; protected set; }
    /// <summary>Gets the Amazon SNS attributes applied to subscriptions targeting the queue.</summary>
    public IDictionary<string, object> QueueSubscriptionAttributes { get; protected set; }
    /// <summary>Gets the tags applied to the queue.</summary>
    public IDictionary<string, string> QueueTags { get; protected set; }
}
