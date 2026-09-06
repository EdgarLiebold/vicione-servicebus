using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job slot released event implementation.
/// </summary>
public class JobSlotReleasedEvent :
    JobSlotReleased
{
    /// <summary>
    /// Gets or sets the job type id value.
    /// </summary>
    public Guid JobTypeId { get; set; }
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the disposition value.
    /// </summary>
    public JobSlotDisposition Disposition { get; set; }
}
