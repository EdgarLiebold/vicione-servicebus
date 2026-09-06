using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the job attempt started event data.</summary>
public class JobAttemptStartedEvent :
    JobAttemptStarted
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the instance address.</summary>
    public Uri InstanceAddress { get; set; } = null!;
}
