using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class CancelJobAttemptCommand :
    CancelJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public string? Reason { get; set; }
}
