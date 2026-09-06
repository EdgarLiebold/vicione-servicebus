using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job attempt completed event implementation.
/// </summary>
public class JobAttemptCompletedEvent :
    JobAttemptCompleted
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; set; }
    /// <summary>
    /// Gets or sets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the instance properties value.
    /// </summary>
    public Dictionary<string, object>? InstanceProperties { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job type properties value.
    /// </summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; } = null!;
}
