using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that an assigned instance began executing a job attempt.</summary>
public interface JobAttemptStarted
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt that started.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the execution start instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the address of the service instance executing the attempt.</summary>
    Uri InstanceAddress { get; }
}
