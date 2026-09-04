using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public interface OutboxConsumeContext :
    ConsumeContext,
    OutboxSendContext
{
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

    Task SetConsumedAsync(CancellationToken cancellationToken = default);

    Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);
}


public interface OutboxConsumeContext<out TMessage> :
    OutboxConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
}
