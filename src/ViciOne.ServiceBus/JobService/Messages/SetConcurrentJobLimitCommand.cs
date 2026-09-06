using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for set concurrent job limit.</summary>
public class SetConcurrentJobLimitCommand :
    SetConcurrentJobLimit
{
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the instance address.</summary>
    public Uri InstanceAddress { get; set; } = null!;
    /// <summary>Gets or sets the concurrent job limit.</summary>
    public int ConcurrentJobLimit { get; set; }
    /// <summary>Gets or sets the kind.</summary>
    public ConcurrentLimitKind Kind { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Gets or sets the job type name.</summary>
    public string? JobTypeName { get; set; }
    /// <summary>Gets or sets the job type properties.</summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
    /// <summary>Gets or sets the instance properties.</summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>Gets or sets the global concurrent job limit.</summary>
    public int? GlobalConcurrentJobLimit { get; set; }
}
