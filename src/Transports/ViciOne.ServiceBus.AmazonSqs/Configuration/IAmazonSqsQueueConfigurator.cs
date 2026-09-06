using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Configures an Amazon SQS queue and its topic subscriptions.</summary>
public interface IAmazonSqsQueueConfigurator
{
    /// <summary>Sets whether the queue is retained when its endpoint stops.</summary>
    bool Durable { set; }

    /// <summary>Sets whether the transport deletes the queue when its endpoint stops.</summary>
    bool AutoDelete { set; }

    /// <summary>Gets optional <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html">Amazon SQS queue attributes</see>.</summary>
    IDictionary<string, object> QueueAttributes { get; }

    /// <summary>Gets optional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS attributes</see> for subscriptions targeting the queue.</summary>
    IDictionary<string, object> QueueSubscriptionAttributes { get; }

    /// <summary>Gets the tags assigned when the queue is created.</summary>
    IDictionary<string, string> QueueTags { get; }
}
