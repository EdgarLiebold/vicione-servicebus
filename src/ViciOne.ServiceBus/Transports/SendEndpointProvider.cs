using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a send endpoint provider implementation.
/// </summary>
public class SendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider,
    IAsyncDisposable
{
    readonly ISendEndpointCache<Uri> _cache;
    readonly ReceiveEndpointContext _context;
    readonly SendObservable _observers;
    readonly ISendTransportProvider _provider;
    readonly ISendPipe _sendPipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="observers">The observers value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    public SendEndpointProvider(ISendTransportProvider provider, SendObservable observers, ReceiveEndpointContext context, ISendPipe sendPipe)
    {
        _provider = provider;
        _sendPipe = sendPipe;

        _observers = observers;
        _context = context;

        _cache = new SendEndpointCache<Uri>();
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        address = _provider.NormalizeAddress(address);

        return _cache.GetSendEndpointAsync(address, CreateSendEndpointAsync, cancellationToken: cancellationToken);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _context.MessageRoutes;

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _observers.Connect(observer);
    }

    async Task<ISendEndpoint> CreateSendEndpointAsync(Uri address)
    {
        var sendTransport = await _provider.GetSendTransportAsync(address).ConfigureAwait(false);

        var handle = sendTransport.ConnectSendObserver(_observers);

        return new SendEndpoint(sendTransport, _context, address, _sendPipe, handle);
    }
}
