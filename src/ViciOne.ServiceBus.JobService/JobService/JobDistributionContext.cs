using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides a snapshot of job-distribution capacity and metadata through read-only collections.</summary>
/// <remarks>Metadata values are copied by reference and may remain mutable.</remarks>
public sealed class JobDistributionContext
{
    internal JobDistributionContext(JobTypeSaga source)
    {
        ArgumentNullException.ThrowIfNull(source);

        JobTypeName = source.Name ?? string.Empty;
        ConcurrentJobLimit = source.OverrideConcurrentJobLimit ?? source.ConcurrentJobLimit;
        JobTypeProperties = new ReadOnlyDictionary<string, object>(JobPropertySnapshot.Create(source.JobTypeProperties));
        ActiveAllocations = Array.AsReadOnly(source.ActiveAllocations.Select(static allocation => new JobAllocationInfo(allocation)).ToArray());
        ServiceInstances = new ReadOnlyDictionary<Uri, JobServiceInstanceInfo>(source.ServiceInstances.ToDictionary(
            static pair => pair.Key,
            static pair => new JobServiceInstanceInfo(pair.Value)));
    }

    /// <summary>Gets the diagnostic name assigned to the job type and consumer endpoint.</summary>
    public string JobTypeName { get; }

    /// <summary>Gets the maximum number of concurrent jobs allocated to each active service instance.</summary>
    public int ConcurrentJobLimit { get; }

    /// <summary>Gets job-type metadata configured through <see cref="JobOptions{TJob}" />.</summary>
    public IReadOnlyDictionary<string, object> JobTypeProperties { get; }

    /// <summary>Gets the active allocations for this job type across all service instances.</summary>
    public IReadOnlyList<JobAllocationInfo> ActiveAllocations { get; }

    /// <summary>Gets service instances whose heartbeat remains within the configured liveness window.</summary>
    public IReadOnlyDictionary<Uri, JobServiceInstanceInfo> ServiceInstances { get; }
}
