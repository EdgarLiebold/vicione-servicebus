using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job started event data.</summary>
public class JobStartedEvent :
    JobStarted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
}


/// <summary>Carries the job started event data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class JobStartedEvent<T> :
    JobStarted<T>
    where T : class
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
