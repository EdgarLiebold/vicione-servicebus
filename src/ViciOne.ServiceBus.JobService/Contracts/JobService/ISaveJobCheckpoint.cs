using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Replaces or clears the durable application checkpoint for the current job attempt.</summary>
public interface ISaveJobCheckpoint
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt allowed to replace the checkpoint.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the serialized checkpoint, or <see langword="null" /> to clear it.</summary>
    IReadOnlyDictionary<string, object>? Checkpoint { get; }
}
