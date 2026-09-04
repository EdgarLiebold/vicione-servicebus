using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for job slot released.
/// </summary>
public interface JobSlotReleased
{
    /// <summary>
    /// Gets the job type id value.
    /// </summary>
    Guid JobTypeId { get; }

    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the disposition value.
    /// </summary>
    JobSlotDisposition Disposition { get; }
}
