using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests cancellation of one active execution attempt.</summary>
[ConfigureConsumeTopology(false)]
public interface CancelJobAttempt
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt to cancel.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the cancellation reason, when one was supplied.</summary>
    string? Reason { get; }
}
