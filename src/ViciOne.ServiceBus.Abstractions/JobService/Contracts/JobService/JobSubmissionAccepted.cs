using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job submission accepted.</summary>
public interface JobSubmissionAccepted
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
}
