using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable monotonic progress update for an execution attempt.</summary>
internal sealed class SetJobProgressCommand :
    SetJobProgress
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long SequenceNumber { get; set; }
    public long Value { get; set; }
    public long? Limit { get; set; }
}
