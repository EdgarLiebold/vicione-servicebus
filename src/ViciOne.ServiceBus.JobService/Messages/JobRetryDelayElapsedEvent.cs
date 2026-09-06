using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable signal that a job retry delay has elapsed.</summary>
internal sealed class JobRetryDelayElapsedEvent :
    JobRetryDelayElapsed
{
    public Guid JobId { get; set; }
}
