using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobAttemptStatusResponse :
    JobAttemptStatus
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public DateTime Timestamp { get; set; }
    public JobStatus Status { get; set; }
}
