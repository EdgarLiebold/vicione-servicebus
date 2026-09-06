using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job slot released.</summary>
public interface JobSlotReleased
{
    /// <summary>Gets the job type id.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the disposition.</summary>
    JobSlotDisposition Disposition { get; }
}
