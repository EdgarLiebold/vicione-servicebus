using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobAttemptCompleted
{
    Guid JobId { get; }
    Guid AttemptId { get; }
    int RetryAttempt { get; }
    DateTimeOffset Timestamp { get; }
    TimeSpan Duration { get; }
    Dictionary<string, object>? InstanceProperties { get; }
    Dictionary<string, object>? JobTypeProperties { get; }
}
