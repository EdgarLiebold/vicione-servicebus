using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request used to reserve execution capacity for a job.</summary>
internal sealed class AllocateJobSlotCommand :
    AllocateJobSlot
{
    public Guid JobTypeId { get; set; }
    public TimeSpan JobTimeout { get; set; }
    public Guid JobId { get; set; }
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
}
