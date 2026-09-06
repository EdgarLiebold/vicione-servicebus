using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the response for job submission accepted.</summary>
public class JobSubmissionAcceptedResponse :
    JobSubmissionAccepted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
