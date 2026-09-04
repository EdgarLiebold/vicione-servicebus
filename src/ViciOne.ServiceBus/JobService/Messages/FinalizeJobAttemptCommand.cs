using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class FinalizeJobAttemptCommand :
    FinalizeJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
}
