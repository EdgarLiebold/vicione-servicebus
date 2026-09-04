using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for cancel job.
/// </summary>
public interface CancelJob
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// The reason for cancelling the job
    /// </summary>
    string? Reason { get; }
}
