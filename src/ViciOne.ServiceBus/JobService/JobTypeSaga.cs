using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;
/// <summary>Every job type has one entry in this state machine.</summary>
public class JobTypeSaga :
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
        Properties = [];
    }

    /// <summary>Gets or sets the current state.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the active job count.</summary>
    public int ActiveJobCount { get; set; }

    /// <summary>
    /// The concurrent job limit, which is configured by the job options. Initially, it defaults to one when the state machine
    /// is created. Once a service endpoint starts, that endpoint sends a command to set the configure concurrent job limit.
    /// </summary>
    public int ConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets the override job limit.</summary>
    public int? OverrideJobLimit { get; set; }

    /// <summary>If an <see cref="OverrideJobLimit" /> is specified, the time when the override job limit expires.</summary>
    public DateTimeOffset? OverrideLimitExpiration { get; set; }

    /// <summary>The last known active jobs.</summary>
    public List<ActiveJob> ActiveJobs { get; set; }

    /// <summary>Tracks the instances, when they were last updated.</summary>
    public Dictionary<Uri, JobTypeInstance> Instances { get; set; }

    /// <summary>Job properties passed by the <see cref="JobOptions{TJob}" /> configuration.</summary>
    public Dictionary<string, object> Properties { get; set; }
    /// <summary>Gets or sets the row version.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the global concurrent job limit.</summary>
    public int? GlobalConcurrentJobLimit { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public int Version { get; set; }

    /// <summary>The name of the job type.</summary>
    public string Name { get; set; } = null!;
    int JobTypeInfo.ConcurrentJobLimit => OverrideJobLimit ?? ConcurrentJobLimit;
    IReadOnlyList<ActiveJob> JobTypeInfo.ActiveJobs => ActiveJobs;
    IReadOnlyDictionary<Uri, JobTypeInstance> JobTypeInfo.Instances => Instances;
    IReadOnlyDictionary<string, object> JobTypeInfo.Properties => Properties ?? [];
    /// <summary>The MD5 hash of the job type.</summary>
    public Guid CorrelationId { get; set; }
}
