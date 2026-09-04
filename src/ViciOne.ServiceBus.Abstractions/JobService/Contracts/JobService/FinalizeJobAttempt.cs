using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for finalize job attempt.
/// </summary>
public interface FinalizeJobAttempt
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Identifies this attempt to run the job
    /// </summary>
    Guid AttemptId { get; }
}
