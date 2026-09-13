using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request that starts the next permitted execution attempt.</summary>
internal sealed class RetryJobCommand :
    IRetryJob
{
    public Guid JobId { get; set; }
}
