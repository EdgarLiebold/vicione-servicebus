using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class SaveJobStateCommand :
    SaveJobState
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public Dictionary<string, object>? JobState { get; set; }
}
