using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Transports in memory message dead letter messages.</summary>
public class InMemoryMessageDeadLetterTransport :
    InMemoryMessageMoveTransport,
    IDeadLetterTransport
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="exchange">The exchange.</param>
    /// <param name="delayProvider">The delay provider.</param>
    public InMemoryMessageDeadLetterTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
