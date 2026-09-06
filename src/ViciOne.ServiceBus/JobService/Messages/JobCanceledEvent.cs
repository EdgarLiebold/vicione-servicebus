using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job canceled event data.</summary>
public class JobCanceledEvent :
    JobCanceled
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string? Reason { get; set; }
}
