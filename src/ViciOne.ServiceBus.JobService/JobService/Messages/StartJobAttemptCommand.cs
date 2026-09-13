using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable execution request sent to the selected service instance.</summary>
internal sealed class StartJobAttemptCommand :
    IStartJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public Uri ServiceAddress { get; set; } = null!;
    public Uri InstanceAddress { get; set; } = null!;
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public Guid JobTypeId { get; set; }
    public long? LastProgressValue { get; set; }
    public long? LastProgressLimit { get; set; }
    public IReadOnlyDictionary<string, object>? Checkpoint { get; set; }
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
}
