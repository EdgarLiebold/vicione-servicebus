using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Each attempt to run a job is tracked by this state.</summary>
public class JobAttemptSaga :
    SagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Gets or sets the current state.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the service address.</summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>Gets or sets the instance address.</summary>
    public Uri InstanceAddress { get; set; } = null!;
    /// <summary>Gets or sets the started.</summary>
    public DateTimeOffset? Started { get; set; }
    /// <summary>Gets or sets the faulted.</summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>Gets or sets the status check token id.</summary>
    public Guid? StatusCheckTokenId { get; set; }

    /// <summary>Gets or sets the row version.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the version.</summary>
    public int Version { get; set; }

    // AttemptId
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid CorrelationId { get; set; }
}
