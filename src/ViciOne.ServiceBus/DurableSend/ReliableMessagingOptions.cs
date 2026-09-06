using System;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class ReliableDeliveryConfigurator : IReliableDeliveryConfigurator
{
    /// <summary>Gets or sets the maximum number of concurrently owned deliveries.</summary>
    public int MaximumConcurrentDeliveries { get; set; } = 16;

    /// <summary>Gets or sets the maximum number of delivery attempts.</summary>
    public int MaximumAttempts { get; set; } = 10;

    /// <summary>Gets or sets the initial retry delay.</summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Gets or sets the maximum retry delay.</summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the bounded fractional retry jitter.</summary>
    public double RetryJitterFraction { get; set; } = 0.20;

    /// <summary>Gets or sets the durable ownership lease duration.</summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets or sets the maximum wait for in-process consumer completion.</summary>
    public TimeSpan ConsumerCompletionTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the idle polling interval.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the telemetry snapshot interval.</summary>
    public TimeSpan TelemetrySnapshotInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the oldest-pending age that degrades health.</summary>
    public TimeSpan HealthDegradedAfter { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>Mutable bootstrap options for one typed bus; validated and frozen before reliable messaging starts.</summary>
public sealed class ReliableMessagingOptions<TBus>
    where TBus : class, IBus
{

    /// <summary>
    /// Gets or sets the maximum stored count value.
    /// </summary>
    public int MaximumStoredCount { get; set; }
    /// <summary>
    /// Gets or sets the maximum stored bytes value.
    /// </summary>
    public long MaximumStoredBytes { get; set; }
    /// <summary>
    /// Gets or sets the maximum concurrent deliveries value.
    /// </summary>
    public int MaximumConcurrentDeliveries { get; set; } = 16;
    /// <summary>
    /// Gets or sets the maximum delivery attempts value.
    /// </summary>
    public int MaximumDeliveryAttempts { get; set; } = 10;
    /// <summary>
    /// Gets or sets the initial retry delay value.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(15);
    /// <summary>
    /// Gets or sets the maximum retry delay value.
    /// </summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// Gets or sets the retry jitter fraction value.
    /// </summary>
    public double RetryJitterFraction { get; set; } = 0.20;
    /// <summary>
    /// Gets or sets the lease duration value.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
    /// <summary>
    /// Gets or sets the consumer completion timeout value.
    /// </summary>
    public TimeSpan ConsumerCompletionTimeout { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// Gets or sets the poll interval value.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Gets or sets the telemetry snapshot interval value.
    /// </summary>
    public TimeSpan TelemetrySnapshotInterval { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// Gets or sets the health degraded after value.
    /// </summary>
    public TimeSpan HealthDegradedAfter { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Gets or sets the retention period for terminal reliable-messaging state.</summary>
    public TimeSpan Retention { get; set; }

    internal bool StoreLimitsConfigured { get; set; }

    internal bool DeliveryConfigured { get; set; }

    internal bool RetentionConfigured { get; set; }

    internal ReliableMessagingPolicy<TBus> ValidateAndFreeze()
    {
        if (!StoreLimitsConfigured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Store limits were not configured.", "Call Store(new ReliableStoreLimits { ... }) inside UseReliableMessaging"));
        if (!DeliveryConfigured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Delivery policy was not configured.", "Call Delivery(...) inside UseReliableMessaging"));
        if (!RetentionConfigured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "Retention was not configured.", "Call Retention(...) inside UseReliableMessaging"));
        if (MaximumStoredCount < 1)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(MaximumStoredCount)} must be positive.", "Correct the named configuration before starting the host"));
        if (MaximumStoredBytes < 1)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(MaximumStoredBytes)} must be positive.", "Correct the named configuration before starting the host"));
        if (MaximumConcurrentDeliveries is < 1 or > DurableSendOperationLimits.AbsoluteMaximumClaimCount)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(MaximumConcurrentDeliveries)} must be between 1 and {DurableSendOperationLimits.AbsoluteMaximumClaimCount}.", "Correct the named configuration before starting the host"));
        if (MaximumDeliveryAttempts < 1)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(MaximumDeliveryAttempts)} must be positive.", "Correct the named configuration before starting the host"));
        if (InitialRetryDelay <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(InitialRetryDelay)} must be positive.", "Correct the named configuration before starting the host"));
        if (MaximumRetryDelay < InitialRetryDelay)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(MaximumRetryDelay)} must not be less than {nameof(InitialRetryDelay)}.", "Correct the named configuration before starting the host"));
        if (RetryJitterFraction is < 0 or > 0.50)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(RetryJitterFraction)} must be between 0 and 0.50.", "Correct the named configuration before starting the host"));
        if (LeaseDuration <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(LeaseDuration)} must be positive.", "Correct the named configuration before starting the host"));
        if (ConsumerCompletionTimeout <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(ConsumerCompletionTimeout)} must be positive.", "Correct the named configuration before starting the host"));
        if (PollInterval <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(PollInterval)} must be positive.", "Correct the named configuration before starting the host"));
        if (TelemetrySnapshotInterval <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(TelemetrySnapshotInterval)} must be positive.", "Correct the named configuration before starting the host"));
        if (HealthDegradedAfter <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(HealthDegradedAfter)} must be positive.", "Correct the named configuration before starting the host"));
        if (Retention <= TimeSpan.Zero)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"{nameof(Retention)} must be positive.", "Correct the named configuration before starting the host"));

        return new ReliableMessagingPolicy<TBus>(
            new DurableSendStoreLimits(MaximumStoredCount, MaximumStoredBytes),
            MaximumConcurrentDeliveries,
            MaximumDeliveryAttempts,
            InitialRetryDelay,
            MaximumRetryDelay,
            RetryJitterFraction,
            LeaseDuration,
            ConsumerCompletionTimeout,
            PollInterval,
            TelemetrySnapshotInterval,
            HealthDegradedAfter,
            Retention);
    }
}

internal sealed record ReliableMessagingPolicy<TBus>(
    DurableSendStoreLimits Limits,
    int MaximumConcurrentDeliveries,
    int MaximumDeliveryAttempts,
    TimeSpan InitialRetryDelay,
    TimeSpan MaximumRetryDelay,
    double RetryJitterFraction,
    TimeSpan LeaseDuration,
    TimeSpan ConsumerCompletionTimeout,
    TimeSpan PollInterval,
    TimeSpan TelemetrySnapshotInterval,
    TimeSpan HealthDegradedAfter,
    TimeSpan Retention)
    where TBus : class, IBus;
