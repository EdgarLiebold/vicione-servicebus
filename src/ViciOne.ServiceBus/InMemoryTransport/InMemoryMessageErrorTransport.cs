using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Transports in memory message error messages.</summary>
public class InMemoryMessageErrorTransport :
    InMemoryMessageMoveTransport,
    IErrorTransport
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="exchange">The exchange.</param>
    /// <param name="delayProvider">The delay provider.</param>
    public InMemoryMessageErrorTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
