using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for finalize job.
/// </summary>
public interface FinalizeJob
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
