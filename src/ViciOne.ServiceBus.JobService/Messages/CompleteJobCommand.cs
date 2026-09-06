using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable completion record produced by a successful job attempt.</summary>
internal sealed class CompleteJobCommand :
    CompleteJob
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public Guid JobTypeId { get; set; }
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
    public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
    public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
}
