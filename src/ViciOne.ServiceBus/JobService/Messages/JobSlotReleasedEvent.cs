using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job slot released event data.</summary>
public class JobSlotReleasedEvent :
    JobSlotReleased
{
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the disposition.</summary>
    public JobSlotDisposition Disposition { get; set; }
}
