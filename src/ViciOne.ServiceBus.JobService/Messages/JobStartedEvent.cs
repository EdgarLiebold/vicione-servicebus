using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the concrete representation used to deserialize an untyped job-started event.</summary>
internal sealed class JobStartedEvent :
    JobStarted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}


/// <summary>Provides the typed event emitted when a job enters execution.</summary>
/// <typeparam name="TJob">The job contract type.</typeparam>
internal sealed class JobStartedEvent<TJob> :
    JobStarted<TJob>
    where TJob : class
{
    public TJob Job { get; set; } = null!;
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
