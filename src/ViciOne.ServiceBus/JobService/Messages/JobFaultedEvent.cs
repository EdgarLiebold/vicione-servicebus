using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job faulted event implementation.
/// </summary>
public class JobFaultedEvent :
    JobFaulted
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the exceptions value.
    /// </summary>
    public ExceptionInfo Exceptions { get; set; } = null!;
}
