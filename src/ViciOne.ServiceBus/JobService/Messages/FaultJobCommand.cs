using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for fault job.</summary>
public class FaultJobCommand :
    FaultJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Gets or sets the exceptions.</summary>
    public ExceptionInfo Exceptions { get; set; } = null!;
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
}
