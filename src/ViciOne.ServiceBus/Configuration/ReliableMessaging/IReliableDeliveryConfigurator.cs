using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures delivery concurrency, retry, lease, polling and health thresholds.</summary>
public interface IReliableDeliveryConfigurator
{
    /// <summary>Gets or sets the maximum number of durable deliveries this host may execute concurrently.</summary>
    int MaximumConcurrentDeliveries { get; set; }

    /// <summary>Gets or sets the total attempt budget, including the initial delivery.</summary>
    int MaximumAttempts { get; set; }

    /// <summary>Gets or sets the delay before the first retry.</summary>
    TimeSpan InitialRetryDelay { get; set; }

    /// <summary>Gets or sets the upper bound for an exponentially increasing retry delay.</summary>
    TimeSpan MaximumRetryDelay { get; set; }

    /// <summary>Gets or sets the symmetric retry-jitter fraction from zero through 0.5.</summary>
    double RetryJitterFraction { get; set; }

    /// <summary>Gets or sets how long a claimed delivery remains exclusively owned by one worker.</summary>
    TimeSpan LeaseDuration { get; set; }

    /// <summary>Gets or sets how long a volatile delivery may await logical consumer completion.</summary>
    TimeSpan ConsumerCompletionTimeout { get; set; }

    /// <summary>Gets or sets the idle delay between durable-store polls.</summary>
    TimeSpan PollInterval { get; set; }

    /// <summary>Gets or sets the minimum interval between retained-state telemetry snapshots.</summary>
    TimeSpan TelemetrySnapshotInterval { get; set; }

    /// <summary>Gets or sets the oldest-pending age at which readiness becomes degraded.</summary>
    TimeSpan HealthDegradedAfter { get; set; }
}
