using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable registration or limit update for a job-service instance.</summary>
internal sealed class SetConcurrentJobLimitCommand :
    ISetConcurrentJobLimit
{
    public Guid JobTypeId { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
    public int ConcurrentJobLimit { get; set; }
    public JobConcurrencyUpdateKind UpdateKind { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? JobTypeName { get; set; }
    public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
    public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
    public int? GlobalConcurrentJobLimit { get; set; }
}
