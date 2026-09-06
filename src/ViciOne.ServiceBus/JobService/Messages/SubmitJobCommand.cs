using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a submit job command implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SubmitJobCommand<T> :
    SubmitJob<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the job value.
    /// </summary>
    public T Job { get; set; } = null!;
    /// <summary>
    /// Gets or sets the schedule value.
    /// </summary>
    public RecurringJobSchedule? Schedule { get; set; }
    /// <summary>
    /// Gets or sets the properties value.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}
