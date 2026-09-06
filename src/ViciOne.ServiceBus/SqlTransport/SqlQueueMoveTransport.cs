using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Transports sql queue move messages.</summary>
public class SqlQueueMoveTransport
{
    readonly string _queueName;
    readonly SqlQueueType _queueType;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    protected SqlQueueMoveTransport(string queueName, SqlQueueType queueType)
    {
        _queueName = queueName;
        _queueType = queueType;
    }

    /// <summary>Moves the current message or entity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="preSend">The pre send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<SqlTransportMessage, SendHeaders> preSend)
    {
        if (!context.TryGetPayload(out SqlMessageContext? messageContext))
            throw new ArgumentException("The ReceiveContext must contain a DbMessageContext", nameof(context));

        if (!context.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The ReceiveContext must contain a ClientContext", nameof(context));

        if (!messageContext.LockId.HasValue)
            throw new ArgumentException("The LockId is not present", nameof(context));

        var message = messageContext.TransportMessage;

        var transportHeaders = SqlTransportMessage.DeserializeHeaders(message.TransportHeaders);

        preSend(message, transportHeaders);

        await clientContext.MoveMessageAsync(messageContext.LockId.Value, messageContext.DeliveryMessageId, _queueName, _queueType,
            message.ExpirationTime, transportHeaders);
    }
}
