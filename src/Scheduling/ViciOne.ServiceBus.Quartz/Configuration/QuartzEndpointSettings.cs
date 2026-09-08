using System;
using Quartz;

namespace ViciOne.ServiceBus.Quartz.Configuration;

internal sealed record QuartzEndpointSettings(
    string QueueName,
    int? PrefetchCount,
    int? ConcurrentMessageLimit,
    Func<string, TimeZoneInfo?>? TimeZoneResolver,
    TimeSpan? StartDelay,
    bool WaitForJobsToComplete,
    RetryPolicy DeliveryRetryPolicy,
    string SchedulerNamespace);
