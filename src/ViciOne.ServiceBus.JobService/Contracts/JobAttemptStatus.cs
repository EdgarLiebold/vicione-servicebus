using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Returns the observed lifecycle status of a job attempt.</summary>
public interface JobAttemptStatus
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the inspected execution attempt.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the instant at which the status was observed.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the observed attempt status.</summary>
    JobAttemptStatusKind Status { get; }
}
