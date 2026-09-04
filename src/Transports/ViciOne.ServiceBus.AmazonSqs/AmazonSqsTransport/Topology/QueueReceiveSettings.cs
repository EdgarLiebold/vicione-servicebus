using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a queue receive settings implementation.
/// </summary>
public class QueueReceiveSettings :
    AmazonSqsQueueSubscriptionConfigurator,
    ReceiveSettings
{
    readonly IAmazonSqsEndpointConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
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

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int ConcurrentMessageLimit => _configuration.Transport.GetConcurrentMessageLimit();

    /// <summary>
    /// Gets or sets the concurrent delivery limit value.
    /// </summary>
    public int ConcurrentDeliveryLimit { get; set; }

    /// <summary>
    /// Gets or sets the wait time seconds value.
    /// </summary>
    public int WaitTimeSeconds { get; set; }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup { get; set; }

    /// <summary>
    /// Gets or sets the is ordered value.
    /// </summary>
    public bool IsOrdered { get; set; }

    /// <summary>
    /// Gets or sets the visibility timeout value.
    /// </summary>
    public int VisibilityTimeout { get; set; }

    /// <summary>
    /// Gets or sets the redeliver visibility timeout value.
    /// </summary>
    public int RedeliverVisibilityTimeout { get; set; }

    /// <summary>
    /// Gets or sets the max visibility timeout value.
    /// </summary>
    public TimeSpan MaxVisibilityTimeout { get; set; }

    /// <summary>
    /// Gets or sets the max visibility timeout renewal value.
    /// </summary>
    public int MaxVisibilityTimeoutRenewal { get; set; }

    /// <summary>
    /// Gets or sets the queue url value.
    /// </summary>
    public string? QueueUrl { get; set; }

    /// <summary>
    /// Gets input address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
