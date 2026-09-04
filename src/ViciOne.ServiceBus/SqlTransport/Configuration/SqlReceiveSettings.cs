using System;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql receive settings implementation.
/// </summary>
public class SqlReceiveSettings :
    SqlQueueConfigurator,
    ReceiveSettings
{
    readonly ISqlEndpointConfiguration _configuration;
    int _concurrentDeliveryLimit;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    public SqlReceiveSettings(ISqlEndpointConfiguration configuration, string queueName, TimeSpan? autoDeleteOnIdle = null)
        : base(queueName, autoDeleteOnIdle)
    {
        _configuration = configuration;

        PollingInterval = TimeSpan.FromSeconds(1);
        ConcurrentDeliveryLimit = 1;

        LockDuration = TimeSpan.FromMinutes(1);
        MaxLockDuration = TimeSpan.FromHours(12);
        MaintenanceBatchSize = 100;
    }

    /// <summary>
    /// Gets or sets the queue id value.
    /// </summary>
    public long? QueueId { get; set; }

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
    public int ConcurrentDeliveryLimit
    {
        get
        {
            return ReceiveMode switch
            {
                SqlReceiveMode.Normal => 1,
                SqlReceiveMode.Partitioned => 1,
                SqlReceiveMode.PartitionedOrdered => 1,
                _ => _concurrentDeliveryLimit
            };
        }
        set => _concurrentDeliveryLimit = value;
    }

    /// <summary>
    /// Gets or sets the receive mode value.
    /// </summary>
    public SqlReceiveMode ReceiveMode { get; set; }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup { get; set; }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan LockDuration { get; set; }

    /// <summary>
    /// Gets or sets the polling interval value.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets the unlock delay value.
    /// </summary>
    public TimeSpan? UnlockDelay { get; set; }

    /// <summary>
    /// Gets or sets the max lock duration value.
    /// </summary>
    public TimeSpan MaxLockDuration { get; set; }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName => QueueName;

    /// <summary>
    /// Gets or sets the maintenance batch size value.
    /// </summary>
    public int MaintenanceBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the dead letter expired messages value.
    /// </summary>
    public bool DeadLetterExpiredMessages { get; set; }

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
