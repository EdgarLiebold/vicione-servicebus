using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures delivery concurrency, retry, lease, polling and health thresholds.</summary>
public interface IReliableDeliveryConfigurator
{
    /// <summary>Gets or sets the maximum concurrent deliveries.</summary>
    int MaximumConcurrentDeliveries { get; set; }

    /// <summary>Gets or sets the maximum attempts.</summary>
    int MaximumAttempts { get; set; }

    /// <summary>Gets or sets the initial retry delay.</summary>
    TimeSpan InitialRetryDelay { get; set; }

    /// <summary>Gets or sets the maximum retry delay.</summary>
    TimeSpan MaximumRetryDelay { get; set; }

    /// <summary>Gets or sets the retry jitter fraction.</summary>
    double RetryJitterFraction { get; set; }

    /// <summary>Gets or sets the lease duration.</summary>
    TimeSpan LeaseDuration { get; set; }

    /// <summary>Gets or sets the consumer completion timeout.</summary>
    TimeSpan ConsumerCompletionTimeout { get; set; }

    /// <summary>Gets or sets the poll interval.</summary>
    TimeSpan PollInterval { get; set; }

    /// <summary>Gets or sets the telemetry snapshot interval.</summary>
    TimeSpan TelemetrySnapshotInterval { get; set; }

    /// <summary>Gets or sets the health degraded after.</summary>
    TimeSpan HealthDegradedAfter { get; set; }
}
