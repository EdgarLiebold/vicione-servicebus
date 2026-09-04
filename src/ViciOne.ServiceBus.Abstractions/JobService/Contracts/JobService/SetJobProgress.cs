using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for set job progress.
/// </summary>
public interface SetJobProgress
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
    /// Gets the sequence number value.
    /// </summary>
    long SequenceNumber { get; }

    /// <summary>
    /// The current job progress value
    /// </summary>
    long Value { get; }

    /// <summary>
    /// The maximum value of job progress (optional)
    /// </summary>
    long? Limit { get; }
}
