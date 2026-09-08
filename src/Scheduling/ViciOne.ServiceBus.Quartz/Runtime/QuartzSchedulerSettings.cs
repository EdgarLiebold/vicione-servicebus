using System;
using Quartz;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Contains validated immutable settings for direct Quartz scheduler configuration.</summary>
internal sealed class QuartzSchedulerSettings
{
    public QuartzSchedulerSettings(ISchedulerFactory schedulerFactory, string queueName,
        int? prefetchCount, int? concurrentMessageLimit, bool startScheduler, TimeSpan? startDelay,
        bool waitForJobsToComplete, TimeProvider timeProvider,
        Func<string, TimeZoneInfo?>? timeZoneResolver, RetryPolicy deliveryRetryPolicy,
        string schedulerNamespace)
    {
        SchedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        if (prefetchCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), prefetchCount, "PrefetchCount must be greater than zero.");
        if (concurrentMessageLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(concurrentMessageLimit),
                concurrentMessageLimit,
                "ConcurrentMessageLimit must be greater than zero.");
        }
        if (startDelay.HasValue && startDelay.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(startDelay), startDelay, "StartDelay must not be negative.");
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        DeliveryRetryPolicy = deliveryRetryPolicy ?? throw new ArgumentNullException(nameof(deliveryRetryPolicy));
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);

        QueueName = queueName;
        PrefetchCount = prefetchCount;
        ConcurrentMessageLimit = concurrentMessageLimit;
        StartScheduler = startScheduler;
        StartDelay = startDelay;
        WaitForJobsToComplete = waitForJobsToComplete;
        TimeZoneResolver = timeZoneResolver;
        SchedulerNamespace = schedulerNamespace;
    }

    public ISchedulerFactory SchedulerFactory { get; }
    public string QueueName { get; }
    public int? PrefetchCount { get; }
    public int? ConcurrentMessageLimit { get; }
    public bool StartScheduler { get; }
    public TimeSpan? StartDelay { get; }
    public bool WaitForJobsToComplete { get; }
    public TimeProvider TimeProvider { get; }
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; }
    public RetryPolicy DeliveryRetryPolicy { get; }
    public string SchedulerNamespace { get; }
}
