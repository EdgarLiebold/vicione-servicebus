using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Wraps addressed send endpoints so operations are deferred by an in-memory outbox.</summary>
internal sealed class InMemoryOutboxSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly OutboxContext _outboxContext;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Initializes the provider over an outbox and a transport send provider.</summary>
    /// <param name="outboxContext">The outbox that defers outgoing operations.</param>
    /// <param name="sendEndpointProvider">The provider that resolves addressed transport endpoints.</param>
    public InMemoryOutboxSendEndpointProvider(OutboxContext outboxContext, ISendEndpointProvider sendEndpointProvider)
    {
        _outboxContext = outboxContext ?? throw new ArgumentNullException(nameof(outboxContext));
        _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
    }

    /// <summary>Registers an observer with the underlying transport send provider.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _sendEndpointProvider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_sendEndpointProvider);

    /// <summary>Resolves and decorates the transport endpoint for a destination address.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task containing an endpoint whose operations are deferred by the outbox.</returns>
    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
