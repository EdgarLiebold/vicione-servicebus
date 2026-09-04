using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

public class ScopedPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly IPublishEndpointProvider _provider;
    readonly IServiceProvider _serviceProvider;

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
