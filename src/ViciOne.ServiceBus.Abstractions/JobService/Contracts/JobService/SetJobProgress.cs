using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by set job progress.</summary>
public interface SetJobProgress
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the sequence number.</summary>
    long SequenceNumber { get; }

    /// <summary>The current job progress value.</summary>
    long Value { get; }

    /// <summary>The maximum value of job progress (optional).</summary>
    long? Limit { get; }
}
