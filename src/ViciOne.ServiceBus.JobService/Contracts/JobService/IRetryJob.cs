using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests another execution attempt for a non-running job.</summary>
public interface IRetryJob
{
    /// <summary>Gets the job for which another attempt is requested.</summary>
    Guid JobId { get; }
}
