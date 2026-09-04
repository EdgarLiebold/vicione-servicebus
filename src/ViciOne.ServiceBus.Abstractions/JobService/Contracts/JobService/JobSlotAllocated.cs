using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job slot allocated.
/// </summary>
public interface JobSlotAllocated
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the instance address value.
    /// </summary>
    Uri InstanceAddress { get; }
}
