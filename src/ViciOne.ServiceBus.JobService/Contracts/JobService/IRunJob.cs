using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests immediate execution of a job that is waiting for its scheduled time.</summary>
public interface IRunJob
{
    /// <summary>Gets the scheduled job to run immediately.</summary>
    Guid JobId { get; }
}
