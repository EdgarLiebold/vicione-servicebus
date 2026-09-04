using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for retry job.
/// </summary>
public interface RetryJob
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
