using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Provides in memory outbox send endpoint services.</summary>
public class InMemoryOutboxSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly OutboxContext _outboxContext;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="outboxContext">The outbox context.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    public InMemoryOutboxSendEndpointProvider(OutboxContext outboxContext, ISendEndpointProvider sendEndpointProvider)
    {
        _outboxContext = outboxContext;
        _sendEndpointProvider = sendEndpointProvider;
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendEndpointProvider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_sendEndpointProvider);

    /// <summary>Gets send endpoint.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
