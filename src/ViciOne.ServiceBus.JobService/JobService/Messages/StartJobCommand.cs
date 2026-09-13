using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable coordination request for a newly allocated job attempt.</summary>
internal sealed class StartJobCommand :
    IStartJob
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public Guid JobTypeId { get; set; }
    public long? LastProgressValue { get; set; }
    public long? LastProgressLimit { get; set; }
    public IReadOnlyDictionary<string, object>? Checkpoint { get; set; }
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
}
