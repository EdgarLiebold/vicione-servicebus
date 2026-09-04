using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job slot wait elapsed.
/// </summary>
public interface JobSlotWaitElapsed
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
}
