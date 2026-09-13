using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable signal requesting supervision of an execution attempt.</summary>
internal sealed class JobStatusCheckRequestedEvent :
    IJobStatusCheckRequested
{
    public Guid AttemptId { get; set; }
    public Guid? JobId { get; set; }
}
