using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Each attempt to run a job is tracked by this state
/// </summary>
public class JobAttemptSaga :
    SagaStateMachineInstance,
    ISagaVersion
{
    public int CurrentState { get; set; }

    public Guid JobId { get; set; }
    public int RetryAttempt { get; set; }
    public Uri ServiceAddress { get; set; } = null!;
    public Uri InstanceAddress { get; set; } = null!;
    public DateTimeOffset? Started { get; set; }
    public DateTimeOffset? Faulted { get; set; }

    public Guid? StatusCheckTokenId { get; set; }

    public byte[] RowVersion { get; set; } = null!;
    public int Version { get; set; }

    // AttemptId
    public Guid CorrelationId { get; set; }
}
