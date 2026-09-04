using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for submit job.
/// </summary>
/// <typeparam name="TJob">The t job type.</typeparam>
public interface SubmitJob<out TJob>
    where TJob : class
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the job value.
    /// </summary>
    TJob Job { get; }

    /// <summary>
    /// Gets the schedule value.
    /// </summary>
    RecurringJobSchedule? Schedule { get; }

    /// <summary>
    /// Gets the properties value.
    /// </summary>
    Dictionary<string, object>? Properties { get; }
}
