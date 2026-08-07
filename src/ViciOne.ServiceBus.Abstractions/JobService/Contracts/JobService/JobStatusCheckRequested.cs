// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    /// <summary>
    /// Signals that the time to supervise a job has expired, and the instance should be checked
    /// </summary>
    public interface JobStatusCheckRequested
    {
        /// <summary>
        /// Identifies this attempt to run the job
        /// </summary>
        Guid AttemptId { get; }

        /// <summary>
        /// Include the jobId for partitioning if available
        /// </summary>
        Guid? JobId { get; }
    }
}
