using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobFaultedEvent :
    JobFaulted
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public Dictionary<string, object> Job { get; set; } = null!;
    public ExceptionInfo Exceptions { get; set; } = null!;
}
