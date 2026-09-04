using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class AllocateJobSlotCommand :
    AllocateJobSlot
{
    public Guid JobTypeId { get; set; }
    public TimeSpan JobTimeout { get; set; }
    public Guid JobId { get; set; }
    public Dictionary<string, object>? JobProperties { get; set; }
}
