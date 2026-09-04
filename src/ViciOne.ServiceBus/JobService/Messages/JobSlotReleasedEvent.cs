using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobSlotReleasedEvent :
    JobSlotReleased
{
    public Guid JobTypeId { get; set; }
    public Guid JobId { get; set; }
    public JobSlotDisposition Disposition { get; set; }
}
