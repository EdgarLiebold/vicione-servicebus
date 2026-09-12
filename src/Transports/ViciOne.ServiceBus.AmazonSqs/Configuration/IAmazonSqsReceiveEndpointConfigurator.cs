using System;
using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Configures an Amazon SQS receive endpoint and its Amazon SNS subscriptions.</summary>
public interface IAmazonSqsReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IAmazonSqsQueueEndpointConfigurator
{
    /// <summary>
    /// The number of seconds to wait before allowing SQS to redeliver the message when faults are returned back to SQS.
    /// Defaults to 1 second.
    /// </summary>
    int RedeliverVisibilityTimeout { set; }

    /// <summary>
    /// Sets the number of concurrent deliveries per <c>MessageGroupId</c>. Values above one increase throughput but permit completion out of order.
    /// The default is one.
    /// This applies to FIFO queues only.
    /// </summary>
    int ConcurrentDeliveryLimit { set; }

    /// <summary>
    /// Sets the maximum total duration for automatic message-visibility renewal.
    /// Must not exceed 12 hours, as per
    /// <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html" />.
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

    /// <summary>Subscribes the receive endpoint queue to the topic for the specified message type.</summary>
    /// <typeparam name="T">The message type whose topic is subscribed.</typeparam>
    /// <param name="callback">An optional callback that configures the topic subscription.</param>
    void Subscribe<T>(Action<IAmazonSqsTopicSubscriptionConfigurator>? callback = null)
        where T : class;

    /// <summary>Subscribes the receive endpoint queue to an Amazon SNS topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="callback">An optional callback that configures the topic subscription.</param>
    void Subscribe(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? callback = null);

    /// <summary>Configures filters in the Amazon client-context pipeline.</summary>
    /// <param name="configure">The callback that updates the client-context pipe.</param>
    void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure);

    /// <summary>Configures filters in the Amazon connection-context pipeline.</summary>
    /// <param name="configure">The callback that updates the connection-context pipe.</param>
    void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>>? configure);

    /// <summary>
    /// Disables raw delivery for queue subscriptions and requires every received body to be a structurally valid Amazon SNS notification envelope.
    /// Use this for queues dedicated to Amazon SNS subscriptions.
    /// </summary>
    void RequireSnsNotificationEnvelope();

    /// <summary>Disables FIFO message-group partitioning and sequence ordering.</summary>
    void DisableMessageOrdering();
}
