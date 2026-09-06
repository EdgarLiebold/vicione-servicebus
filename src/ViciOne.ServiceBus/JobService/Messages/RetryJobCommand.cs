using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for retry job.</summary>
public class RetryJobCommand :
    RetryJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
