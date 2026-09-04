using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class CompleteJobCommand :
    CompleteJob
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Job { get; set; } = null!;
    public Guid JobTypeId { get; set; }
    public Dictionary<string, object>? JobProperties { get; set; }
    public Dictionary<string, object>? InstanceProperties { get; set; }
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}
