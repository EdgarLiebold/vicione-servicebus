using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSubmitted
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }

    Guid JobTypeId { get; }

    /// <summary>
    /// The time the job was submitted
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Timeout when running job
    /// </summary>
    TimeSpan JobTimeout { get; }

    /// <summary>
    /// The job, as an object dictionary
    /// </summary>
    Dictionary<string, object> Job { get; }

    /// <summary>
    /// The job properties
    /// </summary>
    Dictionary<string, object>? JobProperties { get; }

    /// <summary>
    /// If the job is a recurring job, the schedule for the job
    /// </summary>
    RecurringJobSchedule? Schedule { get; }
}
