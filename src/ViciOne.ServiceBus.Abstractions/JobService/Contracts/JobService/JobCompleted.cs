using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Published when a job completes.</summary>
public interface JobCompleted
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the duration.</summary>
    TimeSpan Duration { get; }

    /// <summary>The arguments used to start the job.</summary>
    Dictionary<string, object> Job { get; }

    /// <summary>Properties specified for this job.</summary>
    Dictionary<string, object>? JobProperties { get; }

    /// <summary>Properties of the instance that completed the job.</summary>
    Dictionary<string, object>? InstanceProperties { get; }

    /// <summary>Properties related to the job type.</summary>
    Dictionary<string, object>? JobTypeProperties { get; }
}


/// <summary>Published when a job completes (separately from <see cref="JobCompleted" />).</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface JobCompleted<out T>
    where T : class
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the duration.</summary>
    TimeSpan Duration { get; }

    /// <summary>Gets the job.</summary>
    T Job { get; }

    /// <summary>Properties specified for this job.</summary>
    Dictionary<string, object>? JobProperties { get; }

    /// <summary>Properties of the instance that completed the job.</summary>
    Dictionary<string, object>? InstanceProperties { get; }

    /// <summary>Properties related to the job type.</summary>
    Dictionary<string, object>? JobTypeProperties { get; }
}
