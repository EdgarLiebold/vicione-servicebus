using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job retry delay elapsed.</summary>
public interface JobRetryDelayElapsed
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
}
