using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the response for job slot unavailable.</summary>
public class JobSlotUnavailableResponse :
    JobSlotUnavailable
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
