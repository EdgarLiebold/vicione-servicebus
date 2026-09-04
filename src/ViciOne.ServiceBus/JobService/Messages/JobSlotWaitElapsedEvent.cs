using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobSlotWaitElapsedEvent :
    JobSlotWaitElapsed
{
    public Guid JobId { get; set; }
}
