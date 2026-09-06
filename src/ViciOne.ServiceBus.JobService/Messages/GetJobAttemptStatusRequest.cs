using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable query for the status of a specific execution attempt.</summary>
internal sealed class GetJobAttemptStatusRequest :
    GetJobAttemptStatus
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
}
