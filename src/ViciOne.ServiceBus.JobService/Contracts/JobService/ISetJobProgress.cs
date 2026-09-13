using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Advances the durable progress snapshot of the current job attempt.</summary>
public interface ISetJobProgress
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt reporting progress.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the monotonically increasing progress sequence number.</summary>
    long SequenceNumber { get; }

    /// <summary>Gets the current progress value.</summary>
    long Value { get; }

    /// <summary>Gets the optional upper bound associated with <see cref="Value" />.</summary>
    long? Limit { get; }
}
