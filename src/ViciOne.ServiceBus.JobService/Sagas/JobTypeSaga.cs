using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Persists distributed capacity, instance health, and active allocations for one job type.</summary>
public sealed class JobTypeSaga :
    ISagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Creates empty persisted collections and a single-slot per-instance concurrency default.</summary>
    public JobTypeSaga()
    {
        ConcurrentJobLimit = 1;

        ServiceInstances = new Dictionary<Uri, JobServiceInstanceState>();
        ActiveAllocations = [];
        JobTypeProperties = [];
    }

    /// <summary>Gets or sets the persisted state-machine ordinal.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the number of currently allocated slots.</summary>
    public int ActiveAllocationCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent jobs accepted by each active service instance.
    /// </summary>
    public int ConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets the temporary per-instance concurrency override.</summary>
    public int? OverrideConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets when the temporary concurrency override expires.</summary>
    public DateTimeOffset? OverrideExpiresAt { get; set; }

    /// <summary>Gets or sets the currently allocated jobs.</summary>
    public List<JobAllocationState> ActiveAllocations { get; set; }

    /// <summary>Gets or sets the known service instances keyed by endpoint address.</summary>
    public Dictionary<Uri, JobServiceInstanceState> ServiceInstances { get; set; }

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

    /// <summary>Gets or sets the deterministic identifier of the job type and endpoint.</summary>
    public Guid CorrelationId { get; set; }
}
