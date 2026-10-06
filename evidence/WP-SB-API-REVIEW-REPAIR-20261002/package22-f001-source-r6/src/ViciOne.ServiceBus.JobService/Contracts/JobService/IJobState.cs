using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Describes the coordinator's lifecycle and progress snapshot for a submitted job.</summary>
/// <remarks>The configured saga repository determines persistence and recoverability.</remarks>
public interface IJobState
{
    /// <summary>Gets the job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Gets the submission instant, when the job is known.</summary>
    DateTimeOffset? Submitted { get; }

    /// <summary>Gets the most recent execution start instant.</summary>
    DateTimeOffset? Started { get; }

    /// <summary>Gets the most recent successful completion instant.</summary>
    DateTimeOffset? Completed { get; }

    /// <summary>Gets the duration reported by the most recent completed execution.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Gets the most recent failure or cancellation instant.</summary>
    DateTimeOffset? Faulted { get; }

    /// <summary>Gets the terminal failure or cancellation reason.</summary>
    string? Reason { get; }

    /// <summary>Gets the zero-based number of the most recent attempt.</summary>
    int LastRetryAttempt { get; }

    /// <summary>Gets the current lifecycle phase.</summary>
    JobLifecycleStatus Status { get; }

    /// <summary>Gets the last accepted progress value.</summary>
    long? ProgressValue { get; }

    /// <summary>Gets the optional upper bound associated with <see cref="ProgressValue" />.</summary>
    long? ProgressLimit { get; }

    /// <summary>Gets the serialized application checkpoint in this state snapshot.</summary>
    IReadOnlyDictionary<string, object>? Checkpoint { get; }

    /// <summary>Gets the next scheduled execution instant.</summary>
    DateTimeOffset? NextStartDate { get; }

    /// <summary>Gets a value indicating whether the job has a recurring schedule.</summary>
    bool IsRecurring { get; }

    /// <summary>Gets the earliest execution instant of the schedule.</summary>
    DateTimeOffset? StartDate { get; }

    /// <summary>Gets the last permitted execution instant of the schedule.</summary>
    DateTimeOffset? EndDate { get; }
}


/// <summary>Describes the reported lifecycle and strongly typed checkpoint of a submitted job.</summary>
/// <typeparam name="TCheckpoint">The deserialized checkpoint type.</typeparam>
public interface IJobState<out TCheckpoint> :
    IJobState
    where TCheckpoint : class
{
    /// <summary>Gets the deserialized application checkpoint in this state snapshot.</summary>
    new TCheckpoint? Checkpoint { get; }
}
