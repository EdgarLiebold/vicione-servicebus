using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for get job state.
/// </summary>
public interface GetJobState
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
