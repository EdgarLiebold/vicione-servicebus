using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Stores Amazon SQS queue, polling, concurrency, ordering, and visibility settings for a receive endpoint.</summary>
public class QueueReceiveSettings :
    AmazonSqsQueueSubscriptionConfigurator,
    ReceiveSettings
{
    readonly IAmazonSqsEndpointConfiguration _configuration;

    /// <summary>Initializes receive settings with Amazon SQS polling and visibility defaults.</summary>
    /// <param name="configuration">The endpoint configuration that supplies transport concurrency settings.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="durable">Whether the queue is retained when the endpoint stops.</param>
    /// <param name="autoDelete">Whether the queue is deleted when the endpoint stops.</param>
    public QueueReceiveSettings(IAmazonSqsEndpointConfiguration configuration, string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
        _configuration = configuration;

        WaitTimeSeconds = 3;
        VisibilityTimeout = 30;
        RedeliverVisibilityTimeout = 1;
        MaxVisibilityTimeout = TimeSpan.FromHours(12);
        MaxVisibilityTimeoutRenewal = 60;

        ConcurrentDeliveryLimit = 1;

        if (AmazonSqsEndpointAddress.IsFifo(queueName))
            IsOrdered = true;
    }

    /// <inheritdoc />
    public int PrefetchCount => _configuration.Transport.PrefetchCount;
    /// <inheritdoc />
    public int ConcurrentMessageLimit => _configuration.Transport.GetConcurrentMessageLimit();

    /// <inheritdoc />
    public int ConcurrentDeliveryLimit { get; set; }

    /// <inheritdoc />
    public int WaitTimeSeconds { get; set; }

    /// <inheritdoc />
    public bool PurgeOnStartup { get; set; }

    /// <inheritdoc />
    public bool IsOrdered { get; set; }

    /// <inheritdoc />
    public int VisibilityTimeout { get; set; }

    /// <inheritdoc />
    public int RedeliverVisibilityTimeout { get; set; }

    /// <inheritdoc />
    public TimeSpan MaxVisibilityTimeout { get; set; }

    /// <inheritdoc />
    public int MaxVisibilityTimeoutRenewal { get; set; }

    /// <inheritdoc />
    public string? QueueUrl { get; set; }

    /// <inheritdoc />
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
