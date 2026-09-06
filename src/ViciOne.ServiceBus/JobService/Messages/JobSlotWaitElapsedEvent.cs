using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job slot wait elapsed event data.</summary>
public class JobSlotWaitElapsedEvent :
    JobSlotWaitElapsed
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
