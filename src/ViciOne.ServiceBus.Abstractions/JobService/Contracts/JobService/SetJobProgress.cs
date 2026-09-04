using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

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
