using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Moves faulted messages to an in-memory error exchange.</summary>
internal sealed class InMemoryMessageErrorTransport :
    InMemoryMessageMoveTransport,
    IErrorTransport
{
    /// <summary>Creates an error transport for an exchange.</summary>
    /// <param name="exchange">The error exchange.</param>
    /// <param name="delayProvider">The transport clock used to timestamp delivery.</param>
    public InMemoryMessageErrorTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    /// <summary>Copies a faulted receive envelope and its exception headers to the error exchange.</summary>
    /// <param name="context">The faulted receive context whose envelope is copied.</param>
    /// <param name="cancellationToken">The token that cancels delivery.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        void Prepare(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, Prepare, cancellationToken);
    }
}
