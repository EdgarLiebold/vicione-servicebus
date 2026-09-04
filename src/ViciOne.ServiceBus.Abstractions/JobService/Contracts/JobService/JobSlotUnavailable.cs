using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job slot unavailable.
/// </summary>
public interface JobSlotUnavailable
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
}
