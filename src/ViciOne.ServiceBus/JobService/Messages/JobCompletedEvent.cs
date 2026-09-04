using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job completed event implementation.
/// </summary>
public class JobCompletedEvent :
    JobCompleted
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
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job properties value.
    /// </summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>
    /// Gets or sets the instance properties value.
    /// </summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>
    /// Gets or sets the job type properties value.
    /// </summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}


/// <summary>
/// Provides a job completed event implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class JobCompletedEvent<T> :
    JobCompleted<T>
    where T : class
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
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public T Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job properties value.
    /// </summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>
    /// Gets or sets the instance properties value.
    /// </summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>
    /// Gets or sets the job type properties value.
    /// </summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}
