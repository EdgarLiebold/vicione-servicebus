using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory message error transport implementation.
/// </summary>
public class InMemoryMessageErrorTransport :
    InMemoryMessageMoveTransport,
    IErrorTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="delayProvider">The delay provider value.</param>
    public InMemoryMessageErrorTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
