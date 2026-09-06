using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Isolates persisted job-type state from mutations performed by a custom distribution strategy.</summary>
internal sealed class JobTypeInfoSnapshot :
    JobTypeInfo
{
    /// <summary>Copies all distribution-relevant values from persisted coordinator state.</summary>
    /// <param name="source">The persisted job-type state.</param>
    public JobTypeInfoSnapshot(JobTypeSaga source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Name = source.Name ?? string.Empty;
        ConcurrentJobLimit = source.OverrideJobLimit ?? source.ConcurrentJobLimit;
        JobTypeProperties = JobTypeCapacity.CopyProperties(source.JobTypeProperties);
        ActiveJobs = source.ActiveJobs.Select(static allocation => new ActiveJob
        {
            JobId = allocation.JobId,
            Deadline = allocation.Deadline,
            InstanceAddress = allocation.InstanceAddress,
            JobProperties = JobTypeCapacity.CopyProperties(allocation.JobProperties),
        }).ToArray();
        Instances = source.Instances.ToDictionary(
            static pair => pair.Key,
            static pair => new JobTypeInstance
            {
                Updated = pair.Value.Updated,
                Used = pair.Value.Used,
                InstanceProperties = JobTypeCapacity.CopyProperties(pair.Value.InstanceProperties),
            });
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public int ConcurrentJobLimit { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> JobTypeProperties { get; }

    /// <inheritdoc />
    public IReadOnlyList<ActiveJob> ActiveJobs { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Uri, JobTypeInstance> Instances { get; }
}
