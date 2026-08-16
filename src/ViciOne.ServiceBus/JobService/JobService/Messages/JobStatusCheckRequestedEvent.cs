#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobStatusCheckRequestedEvent :
    JobStatusCheckRequested
{
    public Guid AttemptId { get; set; }
    public Guid? JobId { get; set; }
}
