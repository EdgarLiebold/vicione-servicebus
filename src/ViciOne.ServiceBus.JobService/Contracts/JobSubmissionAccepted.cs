using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Confirms that a job submission was accepted for coordination.</summary>
public interface JobSubmissionAccepted
{
    /// <summary>Gets the accepted job identifier.</summary>
    Guid JobId { get; }
}
