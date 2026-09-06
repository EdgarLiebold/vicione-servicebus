using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job faulted event data.</summary>
public class JobFaultedEvent :
    JobFaulted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the exceptions.</summary>
    public ExceptionInfo Exceptions { get; set; } = null!;
}
