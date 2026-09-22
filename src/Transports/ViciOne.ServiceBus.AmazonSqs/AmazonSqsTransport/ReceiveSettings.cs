using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Defines Amazon SQS queue, polling, concurrency, ordering, and visibility settings for a receive transport.</summary>
public interface ReceiveSettings :
    EntitySettings
{
    /// <summary>Gets the target number of messages prefetched for processing.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the maximum number of messages processed concurrently.</summary>
    int ConcurrentMessageLimit { get; }

    /// <summary>Gets the maximum number of concurrent deliveries within an ordered message group.</summary>
    int ConcurrentDeliveryLimit { get; }

    /// <summary>Gets the Amazon SQS long-poll wait time, in seconds.</summary>
    int WaitTimeSeconds { get; }

    /// <summary>
    /// Gets whether available messages are purged once when this receive endpoint starts.
    /// Reconnecting the same filter instance does not purge the queue again.
    /// </summary>
    bool PurgeOnStartup { get; }

    /// <summary>Gets additional <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html">Amazon SQS queue attributes</see>.</summary>
    IDictionary<string, object> QueueAttributes { get; }

    /// <summary>Gets additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS attributes</see> for subscriptions targeting the queue.</summary>
    IDictionary<string, object> QueueSubscriptionAttributes { get; }

    /// <summary>Gets whether FIFO messages are partitioned by <c>MessageGroupId</c> and ordered by <c>SequenceNumber</c>.</summary>
    bool IsOrdered { get; }

    /// <summary>Gets whether every received body must be an Amazon SNS notification envelope.</summary>
    bool RequiresSnsNotificationEnvelope { get; }

    /// <summary>Gets or sets the queue visibility timeout, in seconds.</summary>
    int VisibilityTimeout { get; set; }

    /// <summary>Gets or sets the maximum total duration for automatic message-visibility renewal.</summary>
    TimeSpan MaxVisibilityTimeout { get; set; }

    /// <summary>Gets or sets the visibility delay, in seconds, applied after message processing faults.</summary>
    int RedeliverVisibilityTimeout { get; set; }

    /// <summary>
    /// The number of seconds to extend the visibility timeout when renewing message visibility during processing.
    /// The endpoint configurator raises values below the 60-second renewal floor to 60 seconds.
    /// Directly assigned settings below that floor fail endpoint validation to avoid excessive renewal traffic.
    /// See <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ChangeMessageVisibility.html">ChangeMessageVisibility</see>.
    /// </summary>
    int MaxVisibilityTimeoutRenewal { get; set; }

    /// <summary>Gets or sets the Amazon SQS queue URL resolved when the receiver starts.</summary>
    string? QueueUrl { get; set; }

    /// <summary>Formats the receive endpoint address relative to an Amazon SQS host.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The queue input address.</returns>
    Uri GetInputAddress(Uri hostAddress);
}
