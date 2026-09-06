using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by retry job.</summary>
public interface RetryJob
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }
}
