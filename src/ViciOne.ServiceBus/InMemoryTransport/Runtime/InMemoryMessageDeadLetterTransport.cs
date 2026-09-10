using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Moves unconsumed messages to an in-memory dead-letter exchange.</summary>
internal sealed class InMemoryMessageDeadLetterTransport :
    InMemoryMessageMoveTransport,
    IDeadLetterTransport
{
    /// <summary>Creates a dead-letter transport for an exchange.</summary>
    /// <param name="exchange">The dead-letter exchange.</param>
    /// <param name="delayProvider">The transport clock used to timestamp delivery.</param>
    public InMemoryMessageDeadLetterTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    /// <summary>Copies an unconsumed receive envelope to the dead-letter exchange.</summary>
    /// <param name="context">The receive context whose envelope is copied.</param>
    /// <param name="reason">The reason recorded on the dead-letter envelope.</param>
    /// <param name="cancellationToken">The token that cancels delivery.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        void Prepare(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, Prepare, cancellationToken);
    }
}
