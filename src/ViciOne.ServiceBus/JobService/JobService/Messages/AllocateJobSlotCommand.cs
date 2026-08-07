// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using System.Collections.Generic;
using Contracts.JobService;


public class AllocateJobSlotCommand :
    AllocateJobSlot
{
    public Guid JobTypeId { get; set; }
    public TimeSpan JobTimeout { get; set; }
    public Guid JobId { get; set; }
    public Dictionary<string, object>? JobProperties { get; set; }
}
