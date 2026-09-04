using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// The context for an outbox instance as part of consume context. Used to signal the completion of
/// the consume, and store any Task factories that should be created.
/// </summary>
public interface OutboxContext
{
    /// <summary>
    /// Returns an awaitable task that is completed when it is clear to send messages
    /// </summary>
    Task ClearToSend { get; }

    /// <summary>
    /// Adds a method to be invoked once the outbox is ready to be sent
    /// </summary>
    /// <param name="method"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures the current pending-operation boundary so an owning transactional attempt can
    /// discard only the messages and schedules added by that attempt if it is rolled back.
    /// </summary>
    OutboxCheckpoint CreateCheckpoint();

    /// <summary>
    /// Execute all the pending outbox operations (success case)
    /// </summary>
    /// <param name="concurrentMessageDelivery"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discard any pending outbox operations, and cancel any scheduled messages
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards operations added after <paramref name="checkpoint" /> while preserving pending
    /// work owned by an earlier successful stage of the same consume pipeline.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="checkpoint">The checkpoint used by the operation.</param>
    Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
