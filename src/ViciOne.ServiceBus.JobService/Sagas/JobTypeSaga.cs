using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Persists distributed capacity, instance health, and active allocations for one job type.</summary>
public sealed class JobTypeSaga :
    SagaStateMachineInstance,
    JobTypeInfo,
    ISagaVersion
{
    /// <summary>Initializes a new instance.</summary>
    public JobTypeSaga()
    {
        ConcurrentJobLimit = 1;

        Instances = new Dictionary<Uri, JobTypeInstance>();
        ActiveJobs = [];
        JobTypeProperties = [];
    }

    /// <summary>Gets or sets the persisted state-machine ordinal.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the number of currently allocated slots.</summary>
    public int ActiveJobCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent jobs accepted by each active service instance.
    /// </summary>
    public int ConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets the temporary per-instance concurrency override.</summary>
    public int? OverrideJobLimit { get; set; }

    /// <summary>Gets or sets when the temporary concurrency override expires.</summary>
    public DateTimeOffset? OverrideLimitExpiration { get; set; }

    /// <summary>Gets or sets the currently allocated jobs.</summary>
    public List<ActiveJob> ActiveJobs { get; set; }

    /// <summary>Gets or sets the known service instances keyed by endpoint address.</summary>
    public Dictionary<Uri, JobTypeInstance> Instances { get; set; }

    /// <summary>Gets or sets metadata configured for the job type.</summary>
    public Dictionary<string, object> JobTypeProperties { get; set; }
    /// <summary>Gets or sets the provider-specific optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the maximum concurrent jobs across all service instances.</summary>
    public int? GlobalConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets the saga revision used for optimistic concurrency.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the diagnostic display name of the job type.</summary>
    public string Name { get; set; } = null!;
    int JobTypeInfo.ConcurrentJobLimit => OverrideJobLimit ?? ConcurrentJobLimit;
    IReadOnlyList<ActiveJob> JobTypeInfo.ActiveJobs => ActiveJobs;
    IReadOnlyDictionary<Uri, JobTypeInstance> JobTypeInfo.Instances => Instances;
    IReadOnlyDictionary<string, object> JobTypeInfo.JobTypeProperties => JobTypeProperties ?? [];
    /// <summary>Gets or sets the deterministic identifier of the job type and endpoint.</summary>
    public Guid CorrelationId { get; set; }
}
