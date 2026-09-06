using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable allocation result identifying the selected service instance.</summary>
internal sealed class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    public Guid JobId { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
