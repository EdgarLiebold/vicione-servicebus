// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService;

using System;


public interface SetJobProgress
{
    Guid JobId { get; }

    Guid AttemptId { get; }

    long SequenceNumber { get; }

    /// <summary>
    /// The current job progress value
    /// </summary>
    long Value { get; }

    /// <summary>
    /// The maximum value of job progress (optional)
    /// </summary>
    long? Limit { get; }
}
