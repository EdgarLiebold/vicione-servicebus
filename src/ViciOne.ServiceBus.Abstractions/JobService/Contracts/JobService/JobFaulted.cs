using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Published when a job faults
/// </summary>
public interface JobFaulted
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the duration value.
    /// </summary>
    TimeSpan? Duration { get; }

    /// <summary>
    /// Gets the job value.
    /// </summary>
    Dictionary<string, object> Job { get; }

    /// <summary>
    /// Gets the exceptions value.
    /// </summary>
    ExceptionInfo Exceptions { get; }
}
