using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Published when a job is canceled.</summary>
public interface JobCanceled
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>The time the job was cancelled.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the cancellation reason, when one was provided.</summary>
    string? Reason { get; }
}
