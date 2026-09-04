using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides an allocate job slot command implementation.
/// </summary>
public class AllocateJobSlotCommand :
    AllocateJobSlot
{
    /// <summary>
    /// Gets or sets the job type id value.
    /// </summary>
    public Guid JobTypeId { get; set; }
    /// <summary>
    /// Gets or sets the job timeout value.
    /// </summary>
    public TimeSpan JobTimeout { get; set; }
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the job properties value.
    /// </summary>
    public Dictionary<string, object>? JobProperties { get; set; }
}
