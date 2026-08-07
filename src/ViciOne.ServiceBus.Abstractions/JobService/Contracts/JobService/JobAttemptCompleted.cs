// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;
    using System.Collections.Generic;


    public interface JobAttemptCompleted
    {
        Guid JobId { get; }
        Guid AttemptId { get; }
        int RetryAttempt { get; }
        DateTime Timestamp { get; }
        TimeSpan Duration { get; }
        Dictionary<string, object>? InstanceProperties { get; }
        Dictionary<string, object>? JobTypeProperties { get; }
    }
}
