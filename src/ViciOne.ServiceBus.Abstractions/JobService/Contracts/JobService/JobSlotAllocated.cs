using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job slot allocated.</summary>
public interface JobSlotAllocated
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the instance address.</summary>
    Uri InstanceAddress { get; }
}
