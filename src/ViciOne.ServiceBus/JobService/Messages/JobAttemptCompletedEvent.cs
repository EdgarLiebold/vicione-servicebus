using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job attempt completed event data.</summary>
public class JobAttemptCompletedEvent :
    JobAttemptCompleted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the instance properties.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; } = null!;
    /// <summary>Gets or sets the job type properties.</summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; } = null!;
}
