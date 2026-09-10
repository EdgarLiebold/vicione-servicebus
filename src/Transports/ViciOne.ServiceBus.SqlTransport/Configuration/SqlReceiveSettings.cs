using System;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines settings for sql receive.</summary>
public class SqlReceiveSettings :
    SqlQueueConfigurator,
    ReceiveSettings
{
    readonly ISqlEndpointConfiguration _configuration;
    int _concurrentDeliveryLimit;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
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

    /// <summary>Gets or sets the queue id.</summary>
    public long? QueueId { get; set; }

    /// <summary>Gets the prefetch count.</summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;

    /// <summary>Gets the concurrent message limit.</summary>
    public int ConcurrentMessageLimit => _configuration.Transport.GetConcurrentMessageLimit();

    /// <summary>Gets or sets the concurrent delivery limit.</summary>
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

    /// <summary>Gets or sets the receive mode.</summary>
    public SqlReceiveMode ReceiveMode { get; set; }

    /// <summary>Gets or sets the purge on startup.</summary>
    public bool PurgeOnStartup { get; set; }

    /// <summary>Gets or sets the lock duration.</summary>
    public TimeSpan LockDuration { get; set; }

    /// <summary>Gets or sets the polling interval.</summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>Gets or sets the unlock delay.</summary>
    public TimeSpan? UnlockDelay { get; set; }

    /// <summary>Gets or sets the max lock duration.</summary>
    public TimeSpan MaxLockDuration { get; set; }

    /// <summary>Gets the entity name.</summary>
    public string EntityName => QueueName;

    /// <summary>Gets or sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize { get; set; }

    /// <summary>Gets or sets the dead letter expired messages.</summary>
    public bool DeadLetterExpiredMessages { get; set; }

    /// <summary>Gets input address.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The input address.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
