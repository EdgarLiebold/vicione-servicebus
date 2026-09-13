using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that an execution attempt acknowledged cancellation.</summary>
public interface IJobAttemptCanceled
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }
    /// <summary>Gets the canceled execution attempt.</summary>
    Guid AttemptId { get; }
    /// <summary>Gets the cancellation instant.</summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>Gets the reason acknowledged by the job consumer.</summary>
    string Reason { get; }
    /// <summary>Gets whether this attempt replaces or clears the persisted checkpoint.</summary>
    bool CheckpointChanged { get; }
    /// <summary>Gets the replacement checkpoint, or <see langword="null" /> when the checkpoint is cleared.</summary>
    IReadOnlyDictionary<string, object>? Checkpoint { get; }
}
