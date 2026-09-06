using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Exposes state for outbox consume operations.</summary>
public interface OutboxConsumeContext :
    ConsumeContext,
    OutboxSendContext
{
    /// <summary>Gets the captured context.</summary>
    ConsumeContext CapturedContext { get; }

    /// <summary>If true, continue processing.</summary>
    bool ContinueProcessing { set; }

    /// <summary>If true, the message was already consumed.</summary>
    bool IsMessageConsumed { get; }

    /// <summary>If true, the outbox messages have already been dispatched.</summary>
    bool IsOutboxDelivered { get; }

    /// <summary>The number of delivery attempts for this message.</summary>
    int ReceiveCount { get; }

    /// <summary>The last sequence number produced from the outbox.</summary>
    long? LastSequenceNumber { get; }

    /// <summary>Sets consumed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetConsumedAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets delivered.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads outbox messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outbox messages outcome.</returns>
    Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reports that notify outbox message has been delivered.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    /// <summary>Removes outbox messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);
}


/// <summary>Exposes state for outbox consume operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface OutboxConsumeContext<out TMessage> :
    OutboxConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
}
