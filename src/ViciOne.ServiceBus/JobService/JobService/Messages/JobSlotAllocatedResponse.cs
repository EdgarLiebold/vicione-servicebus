// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    public Guid JobId { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
