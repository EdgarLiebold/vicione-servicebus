using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job status check requested event implementation.
/// </summary>
public class JobStatusCheckRequestedEvent :
    JobStatusCheckRequested
{
    /// <summary>
    /// Gets or sets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; set; }
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid? JobId { get; set; }
}
