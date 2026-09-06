using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports the successful completion of a job with its serialized payload.</summary>
public interface JobCompleted
{
    /// <summary>Gets the identifier of the completed job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the completion instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the total processing duration.</summary>
    TimeSpan Duration { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the metadata supplied with the job.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }

    /// <summary>Gets the metadata of the service instance that completed the job.</summary>
    IReadOnlyDictionary<string, object>? InstanceProperties { get; }

    /// <summary>Gets the metadata shared by the job type.</summary>
    IReadOnlyDictionary<string, object>? JobTypeProperties { get; }
}


/// <summary>Reports the successful completion of a job with its strongly typed payload.</summary>
/// <typeparam name="TJob">The submitted job type.</typeparam>
public interface JobCompleted<out TJob>
    where TJob : class
{
    /// <summary>Gets the identifier of the completed job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the completion instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the total processing duration.</summary>
    TimeSpan Duration { get; }

    /// <summary>Gets the submitted job payload.</summary>
    TJob Job { get; }

    /// <summary>Gets the metadata supplied with the job.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }

    /// <summary>Gets the metadata of the service instance that completed the job.</summary>
    IReadOnlyDictionary<string, object>? InstanceProperties { get; }

    /// <summary>Gets the metadata shared by the job type.</summary>
    IReadOnlyDictionary<string, object>? JobTypeProperties { get; }
}
