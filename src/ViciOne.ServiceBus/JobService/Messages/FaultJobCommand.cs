using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a fault job command implementation.
/// </summary>
public class FaultJobCommand :
    FaultJob
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; set; }
    /// <summary>
    /// Gets or sets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>
    /// Gets or sets the exceptions value.
    /// </summary>
    public ExceptionInfo Exceptions { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job type id value.
    /// </summary>
    public Guid JobTypeId { get; set; }
}
