using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface GetJobState
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
