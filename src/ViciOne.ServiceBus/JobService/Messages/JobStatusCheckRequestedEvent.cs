using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobStatusCheckRequestedEvent :
    JobStatusCheckRequested
{
    public Guid AttemptId { get; set; }
    public Guid? JobId { get; set; }
}
