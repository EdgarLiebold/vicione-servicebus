using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface FinalizeJob
{
    /// <summary>
    /// The job identifier
    /// </summary>
    Guid JobId { get; }
}
