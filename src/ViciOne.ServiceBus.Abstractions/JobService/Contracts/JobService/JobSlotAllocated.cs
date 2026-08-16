namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface JobSlotAllocated
    {
        Guid JobId { get; }

        Uri InstanceAddress { get; }
    }
}
