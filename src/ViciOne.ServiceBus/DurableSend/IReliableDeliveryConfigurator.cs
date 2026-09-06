using System;

namespace ViciOne.ServiceBus.Configuration;

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
