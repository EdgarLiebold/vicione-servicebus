using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Published when a job faults
/// </summary>
public interface JobFaulted
{
    Guid JobId { get; }

    DateTime Timestamp { get; }

    TimeSpan? Duration { get; }

    Dictionary<string, object> Job { get; }

    ExceptionInfo Exceptions { get; }
}
