using System;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Configures the bus endpoint that accepts Quartz scheduling commands.</summary>
public sealed class QuartzEndpointOptions
{
    /// <summary>Gets or sets the transport prefetch count; <see langword="null"/> leaves it provider-defined.</summary>
    public int? PrefetchCount { get; set; } = 32;

    /// <summary>Gets or sets the maximum number of concurrently processed scheduling commands.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>Gets or sets the scheduling endpoint queue name.</summary>
    public string QueueName { get; set; } = "quartz";

    /// <summary>Gets or sets a fallback resolver for time-zone identifiers unavailable to the operating system.</summary>
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }

    /// <summary>Gets or sets the delay before the scheduler starts after its bus becomes ready.</summary>
    public TimeSpan? StartDelay { get; set; }

    /// <summary>Gets or sets whether final disposal waits for executing jobs to complete.</summary>
    public bool WaitForJobsToComplete { get; set; } = true;

    /// <summary>Gets or sets the persistent backoff applied when delivery fails transiently.</summary>
    public RetryPolicy DeliveryRetryPolicy { get; set; } = RetryPolicy.Exponential(
        maxAttempts: 5,
        initialDelay: TimeSpan.FromSeconds(1),
        factor: 2,
        maxDelay: TimeSpan.FromMinutes(1));

    internal Configuration.QuartzEndpointSettings CreateSettings(Type busType)
    {
        ArgumentNullException.ThrowIfNull(busType);
        ArgumentException.ThrowIfNullOrWhiteSpace(QueueName);
        if (PrefetchCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(PrefetchCount), PrefetchCount, "PrefetchCount must be greater than zero.");
        if (ConcurrentMessageLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ConcurrentMessageLimit),
                ConcurrentMessageLimit,
                "ConcurrentMessageLimit must be greater than zero.");
        }
        if (StartDelay.HasValue && StartDelay.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(StartDelay), StartDelay, "StartDelay must not be negative.");
        ArgumentNullException.ThrowIfNull(DeliveryRetryPolicy);

        return new Configuration.QuartzEndpointSettings(
            QueueName,
            PrefetchCount,
            ConcurrentMessageLimit,
            TimeZoneResolver,
            StartDelay,
            WaitForJobsToComplete,
            DeliveryRetryPolicy,
            Runtime.QuartzSchedulerNamespace.ForBus(busType));
    }
}
