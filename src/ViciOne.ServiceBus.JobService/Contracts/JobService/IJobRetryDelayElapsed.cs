using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Signals that a job's configured retry delay has elapsed.</summary>
public interface IJobRetryDelayElapsed
{
    /// <summary>Gets the job whose delayed retry is now eligible.</summary>
    Guid JobId { get; }
}
