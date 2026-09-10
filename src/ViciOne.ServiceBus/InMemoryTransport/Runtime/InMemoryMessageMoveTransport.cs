using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Copies a received transport envelope to another in-memory exchange.</summary>
internal abstract class InMemoryMessageMoveTransport
{
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    /// <summary>Creates a message mover for a destination exchange.</summary>
    /// <param name="exchange">The exchange that receives the copied envelope.</param>
    /// <param name="delayProvider">The transport clock used to timestamp delivery.</param>
    protected InMemoryMessageMoveTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
    {
        _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <summary>Copies the current receive envelope, applies destination-specific headers, and delivers it.</summary>
    /// <param name="context">The receive context whose envelope is copied.</param>
    /// <param name="prepare">The callback that adds destination-specific headers.</param>
    /// <param name="cancellationToken">The token that cancels delivery to the destination exchange.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task MoveAsync(
        ReceiveContext context,
        Action<InMemoryTransportMessage, SendHeaders> prepare,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(prepare);
        cancellationToken.ThrowIfCancellationRequested();

        var messageId = context.GetMessageId(NewId.NextGuid());

        var body = context.GetBodyBytes();

        var transportMessage = new InMemoryTransportMessage(messageId, body, context.ContentType?.MediaType);

        transportMessage.Headers.SetHostHeaders();

        prepare(transportMessage, transportMessage.Headers);

        var deliveryContext = new InMemoryDeliveryContext(transportMessage, _delayProvider.UtcNow, cancellationToken);

        await _exchange.DeliverAsync(deliveryContext, cancellationToken).ConfigureAwait(false);
    }
}
