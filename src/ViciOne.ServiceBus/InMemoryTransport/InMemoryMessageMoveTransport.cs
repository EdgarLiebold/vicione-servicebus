using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Transports in memory message move messages.</summary>
public class InMemoryMessageMoveTransport
{
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="exchange">The exchange.</param>
    /// <param name="delayProvider">The delay provider.</param>
    protected InMemoryMessageMoveTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
    {
        _exchange = exchange;
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <summary>Moves the current message or entity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="preSend">The pre send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
