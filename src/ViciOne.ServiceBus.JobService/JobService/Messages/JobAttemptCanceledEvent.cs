using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable outcome emitted when an execution attempt is canceled.</summary>
internal sealed class JobAttemptCanceledEvent :
    IJobAttemptCanceled
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Reason { get; set; } = null!;
    public bool CheckpointChanged { get; set; }
    public IReadOnlyDictionary<string, object>? Checkpoint { get; set; }
}
