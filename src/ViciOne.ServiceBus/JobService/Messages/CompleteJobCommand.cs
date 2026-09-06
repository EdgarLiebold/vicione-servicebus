using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for complete job.</summary>
public class CompleteJobCommand :
    CompleteJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>Gets or sets the instance properties.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>Gets or sets the job type properties.</summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}
