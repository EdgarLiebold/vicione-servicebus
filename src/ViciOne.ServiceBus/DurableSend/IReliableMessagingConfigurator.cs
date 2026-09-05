using System;

#nullable enable

namespace ViciOne.ServiceBus.Configuration;
/// <summary>Configures the single reliable-messaging capability owned by a bus.</summary>
public interface IReliableMessagingConfigurator
{
    /// <summary>Adds a stable message contract to the one immutable application catalog.</summary>
    void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class;

    /// <summary>Adds a message contract whose stable identity is declared by <see cref="MessageContractAttribute"/>.</summary>
    void AddMessageContract<TMessage>()
        where TMessage : class;

    /// <summary>Sets the hard retained-record and retained-content limits for the selected store.</summary>
    void Store(ReliableStoreLimits limits);

    /// <summary>Configures the one delivery loop, its retry policy and lease fencing.</summary>
    void Delivery(Action<IReliableDeliveryConfigurator> configure);

    /// <summary>Sets how long terminal inbox and recurring-schedule state is retained.</summary>
    void Retention(TimeSpan duration);
}

/// <summary>Configures delivery concurrency, retry, lease, polling and health thresholds.</summary>
public interface IReliableDeliveryConfigurator
{
    /// <summary>Gets or sets the maximum number of concurrently owned deliveries.</summary>
    int MaximumConcurrentDeliveries { get; set; }

    /// <summary>Gets or sets the maximum number of delivery attempts.</summary>
    int MaximumAttempts { get; set; }

    /// <summary>Gets or sets the initial retry delay.</summary>
    TimeSpan InitialRetryDelay { get; set; }

    /// <summary>Gets or sets the maximum retry delay.</summary>
    TimeSpan MaximumRetryDelay { get; set; }

    /// <summary>Gets or sets the bounded fractional retry jitter.</summary>
    double RetryJitterFraction { get; set; }

    /// <summary>Gets or sets the durable ownership lease duration.</summary>
    TimeSpan LeaseDuration { get; set; }

    /// <summary>Gets or sets the maximum wait for in-process consumer completion.</summary>
    TimeSpan ConsumerCompletionTimeout { get; set; }

    /// <summary>Gets or sets the idle polling interval.</summary>
    TimeSpan PollInterval { get; set; }

    /// <summary>Gets or sets the telemetry snapshot interval.</summary>
    TimeSpan TelemetrySnapshotInterval { get; set; }

    /// <summary>Gets or sets the oldest-pending age that degrades health.</summary>
    TimeSpan HealthDegradedAfter { get; set; }
}

/// <summary>Configures reliable messaging for one typed bus.</summary>
public interface IReliableMessagingConfigurator<TBus> : IReliableMessagingConfigurator
    where TBus : class, IBus
{
}
