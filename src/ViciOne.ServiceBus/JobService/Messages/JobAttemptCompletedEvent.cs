using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobAttemptCompletedEvent :
    JobAttemptCompleted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? InstanceProperties { get; set; } = null!;
    public Dictionary<string, object>? JobTypeProperties { get; set; } = null!;
}
