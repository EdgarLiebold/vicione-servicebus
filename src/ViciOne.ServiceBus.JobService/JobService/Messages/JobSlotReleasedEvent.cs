using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable signal that execution capacity has been released.</summary>
internal sealed class JobSlotReleasedEvent :
    IJobSlotReleased
{
    public Guid JobTypeId { get; set; }
    public Guid JobId { get; set; }
    public JobSlotDisposition Disposition { get; set; }
}
