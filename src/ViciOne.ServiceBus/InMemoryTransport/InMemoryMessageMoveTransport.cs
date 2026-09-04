using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryMessageMoveTransport
{
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    protected InMemoryMessageMoveTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
    {
        _exchange = exchange;
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

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
