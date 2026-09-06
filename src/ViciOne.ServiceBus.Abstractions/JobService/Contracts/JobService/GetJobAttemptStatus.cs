using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by get job attempt status.</summary>
[ConfigureConsumeTopology(false)]
public interface GetJobAttemptStatus
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Identifies this attempt to run the job.</summary>
    Guid AttemptId { get; }
}
