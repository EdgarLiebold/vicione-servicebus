using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job attempt status.</summary>
public interface JobAttemptStatus
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Identifies this attempt to run the job.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the status.</summary>
    JobStatus Status { get; }
}
