using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Each attempt to run a job is tracked by this state
/// </summary>
public class JobAttemptSaga :
    SagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>
    /// Gets or sets the current state value.
    /// </summary>
    public int CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; set; }
    /// <summary>
    /// Gets or sets the service address value.
    /// </summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the instance address value.
    /// </summary>
    public Uri InstanceAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the started value.
    /// </summary>
    public DateTimeOffset? Started { get; set; }
    /// <summary>
    /// Gets or sets the faulted value.
    /// </summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>
    /// Gets or sets the status check token id value.
    /// </summary>
    public Guid? StatusCheckTokenId { get; set; }

    /// <summary>
    /// Gets or sets the row version value.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>
    /// Gets or sets the version value.
    /// </summary>
    public int Version { get; set; }

    // AttemptId
    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public Guid CorrelationId { get; set; }
}
