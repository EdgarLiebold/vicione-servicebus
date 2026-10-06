using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides execution metadata, messaging operations, buffered progress reporting, and checkpoint publication to a job consumer.</summary>
public interface IJobContext :
    PipeContext,
    MessageContext,
    ISendEndpointProvider,
    IPublishEndpoint
{
    /// <summary>Gets the identifier shared by every attempt of this job.</summary>
    Guid JobId { get; }
    /// <summary>Gets the identifier of the current execution attempt.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based number of the current execution attempt.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the last progress value supplied with this execution attempt.</summary>
    long? LastProgressValue { get; }

    /// <summary>Gets the optional limit associated with <see cref="LastProgressValue" />.</summary>
    long? LastProgressLimit { get; }

    /// <summary>Gets the elapsed duration of the current attempt.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets metadata supplied with this job submission.</summary>
    IPropertyCollection JobProperties { get; }

    /// <summary>Gets metadata shared by all consumers of this job type.</summary>
    IPropertyCollection JobTypeProperties { get; }

    /// <summary>Gets metadata of the service instance executing this attempt.</summary>
    IPropertyCollection InstanceProperties { get; }

    /// <summary>Reports an ordered progress snapshot to the job coordinator.</summary>
    /// <param name="value">The current progress value.</param>
    /// <param name="limit">The optional upper bound associated with the value.</param>
    /// <param name="cancellationToken">The token that cancels progress publication.</param>
    /// <returns>A task that completes when the snapshot has entered the attempt's progress buffer.</returns>
    /// <remarks>Updates may be coalesced before publication. Completion does not acknowledge coordinator processing or persistence.</remarks>
    Task ReportProgressAsync(long value, long? limit = null, CancellationToken cancellationToken = default);

    /// <summary>Publishes a checkpoint update for the job coordinator to apply to the current attempt.</summary>
    /// <typeparam name="TCheckpoint">The checkpoint contract type.</typeparam>
    /// <param name="checkpoint">The checkpoint to save, or <see langword="null" /> to clear it.</param>
    /// <param name="cancellationToken">The token that cancels checkpoint publication.</param>
    /// <returns>A task that completes when the configured publication operation has accepted the checkpoint command.</returns>
    /// <remarks>Completion does not acknowledge coordinator processing or a saga-store commit. Recoverability depends on the configured saga repository.</remarks>
    Task SaveCheckpointAsync<TCheckpoint>(TCheckpoint? checkpoint, CancellationToken cancellationToken = default)
        where TCheckpoint : class;

    /// <summary>Attempts to deserialize the checkpoint supplied with this execution attempt.</summary>
    /// <typeparam name="TCheckpoint">The expected checkpoint contract type.</typeparam>
    /// <param name="checkpoint">Receives the deserialized checkpoint when present and compatible.</param>
    /// <returns><see langword="true" /> when a compatible checkpoint is available; otherwise, <see langword="false" />.</returns>
    bool TryGetCheckpoint<TCheckpoint>([NotNullWhen(true)] out TCheckpoint? checkpoint)
        where TCheckpoint : class;
}


/// <summary>Provides job execution operations together with the strongly typed submitted job.</summary>
/// <typeparam name="TJob">The submitted job type.</typeparam>
public interface IJobContext<out TJob> :
    IJobContext
    where TJob : class
{
    /// <summary>The message that initiated the job.</summary>
    TJob Job { get; }
}
