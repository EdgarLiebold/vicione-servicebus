using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable event emitted when an execution attempt starts on an instance.</summary>
internal sealed class JobAttemptStartedEvent :
    IJobAttemptStarted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
