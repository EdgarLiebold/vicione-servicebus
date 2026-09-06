using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides a distribution strategy with the live capacity and metadata of one job type.</summary>
public interface JobTypeInfo
{
    /// <summary>Gets the diagnostic name assigned to the job type and consumer endpoint.</summary>
    string Name { get; }

    /// <summary>Gets the maximum number of concurrent jobs allocated to each active instance.</summary>
    int ConcurrentJobLimit { get; }

    /// <summary>Gets job-type metadata configured through <see cref="JobOptions{TJob}" />.</summary>
    IReadOnlyDictionary<string, object> JobTypeProperties { get; }

    /// <summary>Gets the active allocations for this job type across all service instances.</summary>
    IReadOnlyList<ActiveJob> ActiveJobs { get; }

    /// <summary>Gets service instances whose heartbeat remains within the configured liveness window.</summary>
    IReadOnlyDictionary<Uri, JobTypeInstance> Instances { get; }
}
