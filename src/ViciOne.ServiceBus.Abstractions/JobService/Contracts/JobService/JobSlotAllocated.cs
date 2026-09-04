using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSlotAllocated
{
    Guid JobId { get; }

    Uri InstanceAddress { get; }
}
