using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job submitted event implementation.
/// </summary>
public class JobSubmittedEvent :
    JobSubmitted
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the job type id value.
    /// </summary>
    public Guid JobTypeId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the job timeout value.
    /// </summary>
    public TimeSpan JobTimeout { get; set; }
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the job properties value.
    /// </summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>
    /// Gets or sets the schedule value.
    /// </summary>
    public RecurringJobSchedule? Schedule { get; set; }
}
