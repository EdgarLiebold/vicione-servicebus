using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the response for job slot allocated.</summary>
public class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the instance address.</summary>
    public Uri InstanceAddress { get; set; } = null!;
}
