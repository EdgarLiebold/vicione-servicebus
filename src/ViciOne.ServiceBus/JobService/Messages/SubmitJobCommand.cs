using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for submit job.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SubmitJobCommand<T> :
    SubmitJob<T>
    where T : class
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public T Job { get; set; } = null!;
    /// <summary>Gets or sets the schedule.</summary>
    public RecurringJobSchedule? Schedule { get; set; }
    /// <summary>Gets or sets the properties.</summary>
    public Dictionary<string, object>? Properties { get; set; }
}
