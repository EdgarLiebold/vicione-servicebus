using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobSlotUnavailableResponse :
    JobSlotUnavailable
{
    public Guid JobId { get; set; }
}
