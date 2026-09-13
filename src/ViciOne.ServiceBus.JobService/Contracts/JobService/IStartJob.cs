using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Delivers an assigned job attempt to its target service instance.</summary>
[ConfigureConsumeTopology(false)]
public interface IStartJob
{
    /// <summary>Gets the assigned job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt to start.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the serialized job payload.</summary>
    IReadOnlyDictionary<string, object> Job { get; }

    /// <summary>Gets the stable identity of the job type and endpoint.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the last accepted progress value from an earlier attempt.</summary>
    long? LastProgressValue { get; }

    /// <summary>Gets the optional upper bound associated with <see cref="LastProgressValue" />.</summary>
    long? LastProgressLimit { get; }

    /// <summary>Gets the serialized durable application checkpoint.</summary>
    IReadOnlyDictionary<string, object>? Checkpoint { get; }

    /// <summary>Gets the metadata supplied with the job.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }
}
