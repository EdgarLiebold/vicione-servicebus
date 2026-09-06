using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests the persisted lifecycle state of a job.</summary>
public interface GetJobState
{
    /// <summary>Gets the identifier of the job to inspect.</summary>
    Guid JobId { get; }
}
