using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox send endpoint provider implementation.
/// </summary>
public class InMemoryOutboxSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly OutboxContext _outboxContext;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="outboxContext">The outbox context value.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider value.</param>
    public InMemoryOutboxSendEndpointProvider(OutboxContext outboxContext, ISendEndpointProvider sendEndpointProvider)
    {
        _outboxContext = outboxContext;
        _sendEndpointProvider = sendEndpointProvider;
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendEndpointProvider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_sendEndpointProvider);

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
