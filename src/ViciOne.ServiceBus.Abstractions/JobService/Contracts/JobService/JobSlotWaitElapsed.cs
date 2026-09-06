using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job slot wait elapsed.</summary>
public interface JobSlotWaitElapsed
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
}
