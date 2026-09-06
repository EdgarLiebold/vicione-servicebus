using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for start job.</summary>
public class StartJobCommand :
    StartJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the last progress value carried by this instance.</summary>
    public long? LastProgressValue { get; set; }
    /// <summary>Gets or sets the last progress limit.</summary>
    public long? LastProgressLimit { get; set; }
    /// <summary>Gets or sets the job state.</summary>
    public Dictionary<string, object>? JobState { get; set; }
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
}
