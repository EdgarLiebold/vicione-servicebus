using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable command that replaces or clears a durable application checkpoint.</summary>
internal sealed class SaveJobCheckpointCommand :
    ISaveJobCheckpoint
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public IReadOnlyDictionary<string, object>? Checkpoint { get; set; }
}
