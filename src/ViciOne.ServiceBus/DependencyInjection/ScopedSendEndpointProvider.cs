using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped send endpoint services.</summary>
public class ScopedSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly ISendEndpointProvider _provider;
    readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="serviceProvider">The service provider.</param>
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
