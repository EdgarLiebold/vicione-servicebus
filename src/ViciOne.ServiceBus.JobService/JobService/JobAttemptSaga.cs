using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Persists supervision state for one execution attempt.</summary>
public sealed class JobAttemptSaga :
    ISagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Gets or sets the persisted state-machine ordinal.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the owning job identifier.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the zero-based execution attempt number.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the fallback address shared by all instances of the job service.</summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>Gets or sets the address of the instance assigned to this attempt.</summary>
    public Uri InstanceAddress { get; set; } = null!;
    /// <summary>Gets or sets when execution started.</summary>
    public DateTimeOffset? Started { get; set; }
    /// <summary>Gets or sets when execution supervision detected a failure.</summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>Gets or sets the scheduler token for the pending status check.</summary>
    public Guid? StatusCheckTokenId { get; set; }

    /// <summary>Gets or sets the provider-specific optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the saga revision used for optimistic concurrency.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the attempt identifier used to correlate all supervision messages.</summary>
    public Guid CorrelationId { get; set; }
}
