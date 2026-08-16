namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface JobSlotWaitElapsed
    {
        Guid JobId { get; }
    }
}
