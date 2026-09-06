using System;
using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>
/// Configure a receiving AmazonSQS endpoint
/// </summary>
public interface IAmazonSqsReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IAmazonSqsQueueEndpointConfigurator
{
    /// <summary>
    /// The number of seconds to wait before allowing SQS to redeliver the message when faults are returned back to SQS.
    /// Defaults to 0.
    /// </summary>
    int RedeliverVisibilityTimeout { set; }

    /// <summary>
    /// Set number of concurrent messages per MessageGroupId, higher value will increase throughput but will break delivery order (default: 1).
    /// This applies to FIFO queues only.
    /// </summary>
    int ConcurrentDeliveryLimit { set; }

    /// <summary>
    /// Sets the maximum duration to extend the visibility timeout for a message.
    /// Must not exceed 12 hours, as per
    /// <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html"></see>.
    /// If a value greater than 12 hours is provided, it will be clamped to <c>TimeSpan.FromHours(12)</c>.
    /// Defaults to 12 hours.
    /// </summary>
    public TimeSpan MaxVisibilityTimeout { set; }

    /// <summary>
    /// Sets the number of seconds to extend the visibility timeout when renewing message visibility during processing.
    /// Values less than 60 are raised to the library's 60-second renewal floor to avoid excessive renewal traffic.
    /// Defaults to 60 seconds.
    /// See <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ChangeMessageVisibility.html">ChangeMessageVisibility</see>.
    /// </summary>
    int MaxVisibilityTimeoutRenewal { set; }

    /// <summary>
    /// Subscribes the receive endpoint queue to the topic for the specified message type.
    /// </summary>
    /// <typeparam name="T">The message type whose topic is subscribed.</typeparam>
    void Subscribe<T>(Action<IAmazonSqsTopicSubscriptionConfigurator>? callback = null)
        where T : class;

    /// <summary>
    /// Subscribes the receive endpoint queue to an Amazon SNS topic.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="callback">An optional callback that configures the topic subscription.</param>
    void Subscribe(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? callback = null);

    /// <summary>
    /// Configures client.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure);

    /// <summary>
    /// Configures connection.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>>? configure);

    /// <summary>
    /// FIFO queues deliver messages to consumers partitioned by MessageGroupId, in SequenceNumber order. Calling this method will
    /// disable that behavior.
    /// </summary>
    void DisableMessageOrdering();
}
