using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request used to release an attempt after durable finalization.</summary>
internal sealed class FinalizeJobAttemptCommand :
    FinalizeJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
}
