using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Moves a locked SQL transport delivery to a configured auxiliary queue.</summary>
public class SqlQueueMoveTransport
{
    readonly string _queueName;
    readonly SqlQueueType _queueType;

    /// <summary>Creates a move transport for an auxiliary queue related to <paramref name="queueName" />.</summary>
    /// <param name="queueName">The source queue whose auxiliary queue receives the message.</param>
    /// <param name="queueType">The destination auxiliary queue kind.</param>
    protected SqlQueueMoveTransport(string queueName, SqlQueueType queueType)
    {
        _queueName = queueName;
        _queueType = queueType;
    }

    /// <summary>Applies destination headers and atomically moves the locked delivery.</summary>
    /// <param name="context">The receive context that owns the SQL delivery lock.</param>
    /// <param name="preSend">The callback that updates the headers before the move.</param>
    /// <param name="cancellationToken">The token that cancels the database operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<SqlTransportMessage, SendHeaders> preSend, CancellationToken cancellationToken)
    {
        if (!context.TryGetPayload(out SqlMessageContext? messageContext))
            throw new ArgumentException("The receive context must contain a SQL message context.", nameof(context));

        if (!context.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The receive context must contain a SQL client context.", nameof(context));

        if (!messageContext.LockId.HasValue)
            throw new ArgumentException("The SQL message context does not contain a delivery lock identifier.", nameof(context));

        var message = messageContext.TransportMessage;

        var transportHeaders = SqlTransportMessage.DeserializeHeaders(message.TransportHeaders);

        preSend(message, transportHeaders);

        await clientContext.MoveMessageAsync(messageContext.LockId.Value, messageContext.DeliveryMessageId, _queueName, _queueType,
            message.ExpirationTime, transportHeaders, cancellationToken).ConfigureAwait(false);
    }
}
