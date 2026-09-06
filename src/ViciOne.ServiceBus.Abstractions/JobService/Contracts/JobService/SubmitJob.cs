using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by submit job.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public interface SubmitJob<out TJob>
    where TJob : class
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the job.</summary>
    TJob Job { get; }

    /// <summary>Gets the schedule.</summary>
    RecurringJobSchedule? Schedule { get; }

    /// <summary>Gets the properties.</summary>
    Dictionary<string, object>? Properties { get; }
}
