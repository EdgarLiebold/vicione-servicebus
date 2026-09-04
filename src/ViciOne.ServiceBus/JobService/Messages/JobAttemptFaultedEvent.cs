using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobAttemptFaultedEvent :
    JobAttemptFaulted
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public TimeSpan? RetryDelay { get; set; }
    public DateTime Timestamp { get; set; }
    public ExceptionInfo Exceptions { get; set; } = null!;
}
