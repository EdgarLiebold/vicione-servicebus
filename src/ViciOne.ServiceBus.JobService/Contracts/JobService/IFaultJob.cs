using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Notifies the submitting endpoint that a job attempt ended in failure.</summary>
[ConfigureConsumeTopology(false)]
public interface IFaultJob
{
    /// <summary>Gets the identifier of the faulted job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt that faulted.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the total processing duration when it was measured.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Gets the structured failure details.</summary>
    ExceptionInfo Exceptions { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the stable identity of the job type that processed the job.</summary>
    Guid JobTypeId { get; }
}
