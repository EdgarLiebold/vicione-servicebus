using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests the current execution status of a specific job attempt.</summary>
[ConfigureConsumeTopology(false)]
public interface GetJobAttemptStatus
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt to inspect.</summary>
    Guid AttemptId { get; }
}
