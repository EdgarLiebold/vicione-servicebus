using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by cancel job.</summary>
public interface CancelJob
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>The reason for cancelling the job.</summary>
    string? Reason { get; }
}
