namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox
{
    using System;
    using System.Threading.Tasks;


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
        Task Add(Func<Task> method);

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
        Task ExecutePendingActions(bool concurrentMessageDelivery);

        /// <summary>
        /// Discard any pending outbox operations, and cancel any scheduled messages
        /// </summary>
        /// <returns></returns>
        Task DiscardPendingActions();

        /// <summary>
        /// Discards operations added after <paramref name="checkpoint" /> while preserving pending
        /// work owned by an earlier successful stage of the same consume pipeline.
        /// </summary>
        Task DiscardPendingActions(OutboxCheckpoint checkpoint);
    }
}
