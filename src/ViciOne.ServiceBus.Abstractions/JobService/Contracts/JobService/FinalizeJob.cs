using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by finalize job.</summary>
public interface FinalizeJob
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }
}
