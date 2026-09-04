using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory message move transport implementation.
/// </summary>
public class InMemoryMessageMoveTransport
{
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="delayProvider">The delay provider value.</param>
    protected InMemoryMessageMoveTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
    {
        _exchange = exchange;
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <summary>
    /// Performs the move operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="preSend">The pre send value.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<InMemoryTransportMessage, SendHeaders> preSend)
    {
        var messageId = context.GetMessageId(NewId.NextGuid());

        var body = context.GetBody();

        var transportMessage = new InMemoryTransportMessage(messageId, body, context.ContentType?.MediaType);

        transportMessage.Headers.SetHostHeaders();

        preSend(transportMessage, transportMessage.Headers);

        var deliveryContext = new InMemoryDeliveryContext(transportMessage, _delayProvider.UtcNow, CancellationToken.None);

        await _exchange.DeliverAsync(deliveryContext).ConfigureAwait(false);
    }
}
