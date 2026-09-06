using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job attempt completed.</summary>
public interface JobAttemptCompleted
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }
    /// <summary>Gets the retry attempt.</summary>
    int RetryAttempt { get; }
    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>Gets the duration.</summary>
    TimeSpan Duration { get; }
    /// <summary>Gets the instance properties.</summary>
    Dictionary<string, object>? InstanceProperties { get; }
    /// <summary>Gets the job type properties.</summary>
    Dictionary<string, object>? JobTypeProperties { get; }
}
