using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Starts durable coordination of a serialized job submission.</summary>
public interface IJobSubmitted
{
    /// <summary>Gets the submitted job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Gets the stable identity of the registered job type and endpoint.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the instant at which the job was submitted.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the maximum duration of one execution attempt.</summary>
    TimeSpan JobTimeout { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the optional job metadata.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }

    /// <summary>Gets the optional one-time or recurring execution schedule.</summary>
    IJobSchedule? Schedule { get; }
}
