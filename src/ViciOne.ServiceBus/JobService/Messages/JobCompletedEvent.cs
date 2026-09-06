using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job completed event data.</summary>
public class JobCompletedEvent :
    JobCompleted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>Gets or sets the instance properties.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>Gets or sets the job type properties.</summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}


/// <summary>Carries the job completed event data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class JobCompletedEvent<T> :
    JobCompleted<T>
    where T : class
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public T Job { get; set; } = null!;
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>Gets or sets the instance properties.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>Gets or sets the job type properties.</summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}
