using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobRetryDelayElapsedEvent :
    JobRetryDelayElapsed
{
    public Guid JobId { get; set; }
}
