using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for allocate job slot.</summary>
public class AllocateJobSlotCommand :
    AllocateJobSlot
{
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the job timeout.</summary>
    public TimeSpan JobTimeout { get; set; }
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the job properties.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }
}
