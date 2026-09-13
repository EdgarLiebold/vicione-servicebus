using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Returns a previously allocated execution slot to its job-type coordinator.</summary>
public interface IJobSlotReleased
{
    /// <summary>Gets the stable identity of the job type that owns the slot.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the job that previously owned the slot.</summary>
    Guid JobId { get; }

    /// <summary>Gets the outcome that released the slot.</summary>
    JobSlotDisposition Disposition { get; }
}
