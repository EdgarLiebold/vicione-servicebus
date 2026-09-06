using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports a failed execution attempt and its retry decision.</summary>
public interface JobAttemptFaulted
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }
    /// <summary>Gets the failed execution attempt.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the delay before the next attempt, or <see langword="null" /> when no retry is scheduled.</summary>
    TimeSpan? RetryDelay { get; }

    /// <summary>Gets the failure instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the structured failure details.</summary>
    ExceptionInfo Exceptions { get; }
}
