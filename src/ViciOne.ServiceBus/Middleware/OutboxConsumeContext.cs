using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for outbox consume context.
/// </summary>
public interface OutboxConsumeContext :
    ConsumeContext,
    OutboxSendContext
{
    /// <summary>
    /// Gets the captured context value.
    /// </summary>
    ConsumeContext CapturedContext { get; }

    /// <summary>
    /// If true, continue processing
    /// </summary>
    bool ContinueProcessing { set; }

    /// <summary>
    /// If true, the message was already consumed
    /// </summary>
    bool IsMessageConsumed { get; }

    /// <summary>
    /// If true, the outbox messages have already been dispatched
    /// </summary>
    bool IsOutboxDelivered { get; }

    /// <summary>
    /// The number of delivery attempts for this message
    /// </summary>
    int ReceiveCount { get; }

    /// <summary>
    /// The last sequence number produced from the outbox
    /// </summary>
    long? LastSequenceNumber { get; }

    /// <summary>
    /// Sets consumed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SetConsumedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets delivered.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the load outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the notify outbox message delivered operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the remove outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the contract for outbox consume context.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface OutboxConsumeContext<out TMessage> :
    OutboxConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
}
