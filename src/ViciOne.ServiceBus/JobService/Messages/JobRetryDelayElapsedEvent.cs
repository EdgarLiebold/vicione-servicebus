using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job retry delay elapsed event implementation.
/// </summary>
public class JobRetryDelayElapsedEvent :
    JobRetryDelayElapsed
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
}
