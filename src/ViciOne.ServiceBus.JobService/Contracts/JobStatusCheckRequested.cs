using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Signals that an attempt's liveness lease elapsed and its service instance should be queried.</summary>
public interface JobStatusCheckRequested
{
    /// <summary>Gets the execution attempt to inspect.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the job identifier used to preserve per-job message ordering, when available.</summary>
    Guid? JobId { get; }
}
