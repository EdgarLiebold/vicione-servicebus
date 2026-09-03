#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobAttemptCanceledEvent :
    JobAttemptCanceled
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; } = null!;
}
