using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable signal that a job has exhausted its capacity wait interval.</summary>
internal sealed class JobSlotWaitElapsedEvent :
    JobSlotWaitElapsed
{
    public Guid JobId { get; set; }
}
