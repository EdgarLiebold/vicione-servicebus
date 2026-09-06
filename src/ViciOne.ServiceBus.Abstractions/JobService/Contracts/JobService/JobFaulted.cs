using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Published when a job faults.</summary>
public interface JobFaulted
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the duration.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Gets the job.</summary>
    Dictionary<string, object> Job { get; }

    /// <summary>Gets the exceptions.</summary>
    ExceptionInfo Exceptions { get; }
}
