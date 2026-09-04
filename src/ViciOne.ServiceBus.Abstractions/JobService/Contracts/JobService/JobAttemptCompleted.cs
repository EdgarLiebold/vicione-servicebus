using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job attempt completed.
/// </summary>
public interface JobAttemptCompleted
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
    /// <summary>
    /// Gets the attempt id value.
    /// </summary>
    Guid AttemptId { get; }
    /// <summary>
    /// Gets the retry attempt value.
    /// </summary>
    int RetryAttempt { get; }
    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>
    /// Gets the duration value.
    /// </summary>
    TimeSpan Duration { get; }
    /// <summary>
    /// Gets the instance properties value.
    /// </summary>
    Dictionary<string, object>? InstanceProperties { get; }
    /// <summary>
    /// Gets the job type properties value.
    /// </summary>
    Dictionary<string, object>? JobTypeProperties { get; }
}
