using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job slot allocated response implementation.
/// </summary>
public class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the instance address value.
    /// </summary>
    public Uri InstanceAddress { get; set; } = null!;
}
