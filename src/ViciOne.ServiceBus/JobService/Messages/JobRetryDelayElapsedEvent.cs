using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job retry delay elapsed event data.</summary>
public class JobRetryDelayElapsedEvent :
    JobRetryDelayElapsed
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
