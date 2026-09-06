using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job attempt faulted.</summary>
public interface JobAttemptFaulted
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }

    /// <summary>The retry attempt that faulted. Zero for the first attempt.</summary>
    int RetryAttempt { get; }

    /// <summary>If present, the delay until the next retry.</summary>
    TimeSpan? RetryDelay { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the exceptions.</summary>
    ExceptionInfo Exceptions { get; }
}
