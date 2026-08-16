namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface JobRetryDelayElapsed
    {
        Guid JobId { get; }
    }
}
