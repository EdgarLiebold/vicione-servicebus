using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Releases the persisted coordinator state for a terminal job.</summary>
public interface FinalizeJob
{
    /// <summary>Gets the identifier of the terminal job to remove.</summary>
    Guid JobId { get; }
}
