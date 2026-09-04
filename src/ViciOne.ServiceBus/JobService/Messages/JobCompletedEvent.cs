using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobCompletedEvent :
    JobCompleted
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Job { get; set; } = null!;
    public Dictionary<string, object>? JobProperties { get; set; }
    public Dictionary<string, object>? InstanceProperties { get; set; }
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}


public class JobCompletedEvent<T> :
    JobCompleted<T>
    where T : class
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public T Job { get; set; } = null!;
    public Dictionary<string, object>? JobProperties { get; set; }
    public Dictionary<string, object>? InstanceProperties { get; set; }
    public Dictionary<string, object>? JobTypeProperties { get; set; }
}
