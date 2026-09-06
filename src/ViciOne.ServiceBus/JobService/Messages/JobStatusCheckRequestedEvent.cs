using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job status check requested event data.</summary>
public class JobStatusCheckRequestedEvent :
    JobStatusCheckRequested
{
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the job id.</summary>
    public Guid? JobId { get; set; }
}
