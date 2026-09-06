using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job retry delay elapsed.
/// </summary>
public interface JobRetryDelayElapsed
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
}
