namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface RetryJob
    {
        /// <summary>
        /// The job identifier
        /// </summary>
        Guid JobId { get; }
    }
}
