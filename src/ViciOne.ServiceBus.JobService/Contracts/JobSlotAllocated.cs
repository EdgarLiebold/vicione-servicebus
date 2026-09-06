using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Confirms that an execution slot was assigned to a job.</summary>
public interface JobSlotAllocated
{
    /// <summary>Gets the job that owns the allocated slot.</summary>
    Guid JobId { get; }

    /// <summary>Gets the address of the service instance assigned to the job.</summary>
    Uri InstanceAddress { get; }
}
