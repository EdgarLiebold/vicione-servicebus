using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobAttemptStatus
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Identifies this attempt to run the job
    /// </summary>
    Guid AttemptId { get; }

    DateTime Timestamp { get; }

    JobStatus Status { get; }
}
