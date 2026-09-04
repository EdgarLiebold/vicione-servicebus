using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    public Guid JobId { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
