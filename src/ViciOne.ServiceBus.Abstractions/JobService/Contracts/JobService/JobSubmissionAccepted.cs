using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job submission accepted.
/// </summary>
public interface JobSubmissionAccepted
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
}
