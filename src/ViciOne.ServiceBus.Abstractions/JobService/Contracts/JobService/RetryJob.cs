using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface RetryJob
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
