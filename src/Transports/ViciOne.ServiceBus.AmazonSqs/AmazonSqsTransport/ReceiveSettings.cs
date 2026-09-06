using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>
/// Specify the receive settings for a receive transport
/// </summary>
public interface ReceiveSettings :
    EntitySettings
{
    /// <summary>
    /// The number of unacknowledged messages to allow to be processed concurrently
    /// </summary>
    int PrefetchCount { get; }

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets the concurrent delivery limit value.
    /// </summary>
    int ConcurrentDeliveryLimit { get; }

    /// <summary>
    /// Gets the wait time seconds value.
    /// </summary>
    int WaitTimeSeconds { get; }

    /// <summary>
    /// If True, and a queue name is specified, if the queue exists and has messages, they are purged at startup
    /// If the connection is reset, messages are not purged until the service is reset
    /// </summary>
    bool PurgeOnStartup { get; }

    /// <summary>
    /// Additional <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html">attributes</see> for the queue.
    /// </summary>
    IDictionary<string, object> QueueAttributes { get; }

    /// <summary>
    /// Additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">attributes</see> for the queue's subscription.
    /// </summary>
    IDictionary<string, object> QueueSubscriptionAttributes { get; }

    /// <summary>
    /// If the queue is ordered, enables grouping by MessageGroupId and process messages in ordered way by SequenceNumber
    /// </summary>
    bool IsOrdered { get; }

    /// <summary>
    /// Gets or sets the visibility timeout value.
    /// </summary>
    int VisibilityTimeout { get; set; }

    /// <summary>
    /// Gets or sets the max visibility timeout value.
    /// </summary>
    TimeSpan MaxVisibilityTimeout { get; set; }

    /// <summary>
    /// The number of seconds to wait before allowing SQS to redeliver the message when faults are returned back to SQS.
    /// </summary>
    int RedeliverVisibilityTimeout { get; set; }

    /// <summary>
    /// The number of seconds to extend the visibility timeout when renewing message visibility during processing.
    /// Values below the library's 60-second renewal floor are raised to 60 seconds to avoid excessive renewal traffic.
    /// See <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ChangeMessageVisibility.html">ChangeMessageVisibility</see>.
    /// </summary>
    int MaxVisibilityTimeoutRenewal { get; set; }

    /// <summary>
    /// Gets or sets the queue url value.
    /// </summary>
    string? QueueUrl { get; set; }

    /// <summary>
    /// Get the input address for the transport on the specified host
    /// </summary>
    Uri GetInputAddress(Uri hostAddress);
}
