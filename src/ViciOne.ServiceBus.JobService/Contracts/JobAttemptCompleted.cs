using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports the successful completion of an execution attempt.</summary>
public interface JobAttemptCompleted
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }
    /// <summary>Gets the completed execution attempt.</summary>
    Guid AttemptId { get; }
    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }
    /// <summary>Gets the completion instant.</summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>Gets the attempt duration.</summary>
    TimeSpan Duration { get; }
    /// <summary>Gets the metadata of the service instance that completed the attempt.</summary>
    IReadOnlyDictionary<string, object>? InstanceProperties { get; }
    /// <summary>Gets the metadata shared by the job type.</summary>
    IReadOnlyDictionary<string, object>? JobTypeProperties { get; }
    /// <summary>Gets whether this attempt replaces or clears the persisted checkpoint.</summary>
    bool CheckpointChanged { get; }
    /// <summary>Gets the replacement checkpoint, or <see langword="null" /> when the checkpoint is cleared.</summary>
    IReadOnlyDictionary<string, object>? Checkpoint { get; }
}
