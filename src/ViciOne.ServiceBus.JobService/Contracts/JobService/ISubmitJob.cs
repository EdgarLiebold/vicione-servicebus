using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Submits a strongly typed job for durable execution or scheduling.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public interface ISubmitJob<out TJob>
    where TJob : class
{
    /// <summary>Gets the stable identifier assigned to the job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the job payload.</summary>
    TJob Job { get; }

    /// <summary>Gets the optional one-time or recurring execution schedule.</summary>
    IJobSchedule? Schedule { get; }

    /// <summary>Gets the optional metadata supplied with the job.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }
}
