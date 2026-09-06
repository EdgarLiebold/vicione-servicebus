using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable outcome emitted when an execution attempt fails.</summary>
internal sealed class JobAttemptFaultedEvent :
    JobAttemptFaulted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public TimeSpan? RetryDelay { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public ExceptionInfo Exceptions { get; set; } = null!;
}
