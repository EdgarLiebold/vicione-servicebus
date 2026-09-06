using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by get job state.</summary>
public interface GetJobState
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }
}
