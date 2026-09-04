using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped publish endpoint provider implementation.
/// </summary>
public class ScopedPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly IPublishEndpointProvider _provider;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="serviceProvider">The service provider value.</param>
    public ScopedPublishEndpointProvider(IPublishEndpointProvider provider, IServiceProvider serviceProvider)
    {
        _provider = provider;
        _serviceProvider = serviceProvider;
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        return _provider.ConnectPublishObserver(observer);
    }

    async Task<ISendEndpoint> IPublishEndpointProvider.GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken)
        where T : class
    {
        var endpoint = await _provider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ScopedSendEndpoint(endpoint, _serviceProvider);
    }
}
