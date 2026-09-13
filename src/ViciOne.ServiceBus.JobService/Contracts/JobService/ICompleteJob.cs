using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Notifies the submitting endpoint that a job completed successfully.</summary>
[ConfigureConsumeTopology(false)]
public interface ICompleteJob
{
    /// <summary>Gets the identifier of the completed job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the completion instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the total processing duration.</summary>
    TimeSpan Duration { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the stable identity of the job type that completed the job.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the metadata supplied with the job.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }

    /// <summary>Gets the metadata of the service instance that completed the job.</summary>
    IReadOnlyDictionary<string, object>? InstanceProperties { get; }

    /// <summary>Gets the metadata shared by the job type.</summary>
    IReadOnlyDictionary<string, object>? JobTypeProperties { get; }
}
