using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job slot unavailable.</summary>
public interface JobSlotUnavailable
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
}
