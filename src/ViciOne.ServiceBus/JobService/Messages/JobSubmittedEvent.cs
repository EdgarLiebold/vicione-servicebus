using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobSubmittedEvent :
    JobSubmitted
{
    public Guid JobId { get; set; }
    public Guid JobTypeId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan JobTimeout { get; set; }
    public Dictionary<string, object> Job { get; set; } = null!;
    public Dictionary<string, object>? JobProperties { get; set; }
    public RecurringJobSchedule? Schedule { get; set; }
}
