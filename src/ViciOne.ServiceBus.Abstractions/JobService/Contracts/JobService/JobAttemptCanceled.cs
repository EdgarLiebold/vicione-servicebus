using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job attempt canceled.
/// </summary>
public interface JobAttemptCanceled
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
    /// Gets the timestamp value.
    /// </summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>
    /// Gets the reason value.
    /// </summary>
    string Reason { get; }
}
