using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a set concurrent job limit command implementation.
/// </summary>
public class SetConcurrentJobLimitCommand :
    SetConcurrentJobLimit
{
    /// <summary>
    /// Gets or sets the job type id value.
    /// </summary>
    public Guid JobTypeId { get; set; }
    /// <summary>
    /// Gets or sets the instance address value.
    /// </summary>
    public Uri InstanceAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the concurrent job limit value.
    /// </summary>
    public int ConcurrentJobLimit { get; set; }
    /// <summary>
    /// Gets or sets the kind value.
    /// </summary>
    public ConcurrentLimitKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>
    /// Gets or sets the job type name value.
    /// </summary>
    public string? JobTypeName { get; set; }
    /// <summary>
    /// Gets or sets the job type properties value.
    /// </summary>
    public Dictionary<string, object>? JobTypeProperties { get; set; }
    /// <summary>
    /// Gets or sets the instance properties value.
    /// </summary>
    public Dictionary<string, object>? InstanceProperties { get; set; }
    /// <summary>
    /// Gets or sets the global concurrent job limit value.
    /// </summary>
    public int? GlobalConcurrentJobLimit { get; set; }
}
