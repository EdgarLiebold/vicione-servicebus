using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable query for the durable state of a job.</summary>
internal sealed class GetJobStateRequest :
    IGetJobState
{
    public Guid JobId { get; set; }
}
