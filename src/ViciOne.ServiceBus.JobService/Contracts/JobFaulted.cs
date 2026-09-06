using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that a job reached its faulted terminal state.</summary>
public interface JobFaulted
{
    /// <summary>Gets the identifier of the faulted job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the terminal failure instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the total processing duration when it was measured.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the structured terminal failure details.</summary>
    ExceptionInfo Exceptions { get; }
}
