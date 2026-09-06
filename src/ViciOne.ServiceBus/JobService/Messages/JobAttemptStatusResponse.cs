using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the response for job attempt status.</summary>
public class JobAttemptStatusResponse :
    JobAttemptStatus
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public JobStatus Status { get; set; }
}
