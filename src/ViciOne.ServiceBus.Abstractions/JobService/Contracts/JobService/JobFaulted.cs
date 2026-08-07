// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;
    using System.Collections.Generic;


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
}
