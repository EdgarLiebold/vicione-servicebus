using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job attempt faulted.
/// </summary>
public interface JobAttemptFaulted
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
    /// The retry attempt that faulted. Zero for the first attempt.
    /// </summary>
    int RetryAttempt { get; }

    /// <summary>
    /// If present, the delay until the next retry
    /// </summary>
    TimeSpan? RetryDelay { get; }

    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the exceptions value.
    /// </summary>
    ExceptionInfo Exceptions { get; }
}
