using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request used to cancel one execution attempt.</summary>
internal sealed class CancelJobAttemptCommand :
    ICancelJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public string? Reason { get; set; }
}
