// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


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
