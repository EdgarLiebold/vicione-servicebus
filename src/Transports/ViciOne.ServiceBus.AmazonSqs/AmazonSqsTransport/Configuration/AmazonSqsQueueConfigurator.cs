using System.Collections.Generic;
using Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs queue configurator implementation.
/// </summary>
public class AmazonSqsQueueConfigurator :
    EntityConfigurator,
    IAmazonSqsQueueConfigurator,
    Queue
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="queueAttributes">The queue attributes value.</param>
    /// <param name="queueSubscriptionAttributes">The queue subscription attributes value.</param>
    /// <param name="queueTags">The queue tags value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    public AmazonSqsQueueConfigurator(Queue source)
        : base(source.EntityName, source.Durable, source.AutoDelete)
    {
        QueueAttributes = source.QueueAttributes;
        QueueSubscriptionAttributes = source.QueueSubscriptionAttributes;
        QueueTags = source.QueueTags;
    }

    /// <summary>
    /// Gets the tags value.
    /// </summary>
    public IDictionary<string, string> Tags => QueueTags;

    /// <summary>
    /// Gets the address type value.
    /// </summary>
    protected override AmazonSqsEndpointAddress.AddressType AddressType => AmazonSqsEndpointAddress.AddressType.Queue;

    /// <summary>
    /// Gets or sets the queue attributes value.
    /// </summary>
    public IDictionary<string, object> QueueAttributes { get; protected set; }
    /// <summary>
    /// Gets or sets the queue subscription attributes value.
    /// </summary>
    public IDictionary<string, object> QueueSubscriptionAttributes { get; protected set; }
    /// <summary>
    /// Gets or sets the queue tags value.
    /// </summary>
    public IDictionary<string, string> QueueTags { get; protected set; }
}
