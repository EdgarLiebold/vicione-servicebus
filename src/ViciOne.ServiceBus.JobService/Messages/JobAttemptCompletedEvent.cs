using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable outcome emitted when an execution attempt completes successfully.</summary>
internal sealed class JobAttemptCompletedEvent :
    JobAttemptCompleted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
    public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
}
