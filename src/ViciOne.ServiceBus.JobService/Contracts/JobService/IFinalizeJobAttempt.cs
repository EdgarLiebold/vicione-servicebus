using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Releases the persisted coordinator state for one job attempt.</summary>
public interface IFinalizeJobAttempt
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt whose coordinator state is removed.</summary>
    Guid AttemptId { get; }
}
