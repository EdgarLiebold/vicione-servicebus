using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;
/// <summary>Describes an Amazon SQS queue declaration.</summary>
public interface Queue
{
    /// <summary>Gets the queue name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the queue is retained beyond the bus lifetime.</summary>
    bool Durable { get; }

    /// <summary>Gets whether the transport deletes the queue when its endpoint stops.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets additional <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html">Amazon SQS queue attributes</see>.</summary>
    IDictionary<string, object> QueueAttributes { get; }

    /// <summary>Gets additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS attributes</see> for subscriptions targeting the queue.</summary>
    IDictionary<string, object> QueueSubscriptionAttributes { get; }

    /// <summary>Gets the tags assigned when the queue is created.</summary>
    IDictionary<string, string> QueueTags { get; }
}
