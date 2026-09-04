using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobAttemptStartedEvent :
    JobAttemptStarted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
