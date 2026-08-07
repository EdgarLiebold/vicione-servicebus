// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using System.Collections.Generic;
using Contracts.JobService;


public class SaveJobStateCommand :
    SaveJobState
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public Dictionary<string, object>? JobState { get; set; }
}
