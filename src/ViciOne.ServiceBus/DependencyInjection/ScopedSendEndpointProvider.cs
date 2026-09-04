using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped send endpoint provider implementation.
/// </summary>
public class ScopedSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly ISendEndpointProvider _provider;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="serviceProvider">The service provider value.</param>
    public ScopedSendEndpointProvider(ISendEndpointProvider provider, IServiceProvider serviceProvider)
    {
        _provider = provider;
        _serviceProvider = serviceProvider;
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        return _provider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_provider);

    async Task<ISendEndpoint> ISendEndpointProvider.GetSendEndpointAsync(Uri address, CancellationToken cancellationToken)
    {
        var endpoint = await _provider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ScopedSendEndpoint(endpoint, _serviceProvider);
    }
}
