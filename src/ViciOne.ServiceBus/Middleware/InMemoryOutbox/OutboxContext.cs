using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Buffers outgoing operations for one consume context and controls whether they execute or are discarded.
/// </summary>
public interface OutboxContext
{
    /// <summary>Gets a task that completes when buffered operations may be delivered.</summary>
    Task ClearToSend { get; }

    /// <summary>Adds an asynchronous operation to be invoked after successful consumption.</summary>
    /// <param name="method">The operation to defer.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has been queued or invoked.</returns>
    Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures the current pending-operation boundary so later additions can be discarded independently.
    /// </summary>
    /// <returns>An opaque checkpoint owned by this outbox.</returns>
    OutboxCheckpoint CreateCheckpoint();

    /// <summary>Releases and executes every pending outbox operation.</summary>
    /// <param name="concurrentMessageDelivery">Whether independent deferred sends may execute concurrently.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when every pending operation has finished.</returns>
    Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default);

    /// <summary>Discards pending operations and requests cancellation of tracked scheduled messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task representing pending-work discard and schedule-cancellation processing.</returns>
    Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards operations added after <paramref name="checkpoint"/> while preserving earlier pending work.
    /// </summary>
    /// <param name="checkpoint">The checkpoint that defines the state to retain.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when work added after the checkpoint has been discarded.</returns>
    Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
