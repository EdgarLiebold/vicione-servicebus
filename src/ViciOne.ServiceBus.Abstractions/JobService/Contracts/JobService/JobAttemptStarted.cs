using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobAttemptStarted
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Identifies this attempt to run the job
    /// </summary>
    Guid AttemptId { get; }

    /// <summary>
    /// Zero if the job is being started for the first time, otherwise, the number of previous failures
    /// </summary>
    int RetryAttempt { get; }

    /// <summary>
    /// The time the job was started
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The address of the instance on which this job was started
    /// </summary>
    Uri InstanceAddress { get; }
}
