using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serialized job payload and metadata emitted after successful completion.</summary>
internal sealed class JobCompletedEvent :
    JobCompleted
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
    public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
    public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
}


/// <summary>Provides the typed job payload and metadata emitted after successful completion.</summary>
/// <typeparam name="TJob">The job contract type.</typeparam>
internal sealed class JobCompletedEvent<TJob> :
    JobCompleted<TJob>
    where TJob : class
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public TJob Job { get; set; } = null!;
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
    public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
    public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
}
