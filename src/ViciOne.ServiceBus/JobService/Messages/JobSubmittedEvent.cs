using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job submitted event data.</summary>
public class JobSubmittedEvent :
    JobSubmitted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the job timeout.</summary>
    public TimeSpan JobTimeout { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
    /// <summary>Gets or sets the schedule.</summary>
    public RecurringJobSchedule? Schedule { get; set; }
}
