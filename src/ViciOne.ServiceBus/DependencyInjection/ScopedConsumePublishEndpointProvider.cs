using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped consume publish endpoint services.</summary>
public class ScopedConsumePublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly ConsumeContext _consumeContext;
    readonly IPublishEndpointProvider _provider;
    readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ScopedConsumePublishEndpointProvider(IPublishEndpointProvider provider, ConsumeContext consumeContext, IServiceProvider serviceProvider)
    {
        _provider = provider;
        _consumeContext = consumeContext;
        _serviceProvider = serviceProvider;
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        return _provider.ConnectPublishObserver(observer);
    }

    async Task<ISendEndpoint> IPublishEndpointProvider.GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken)
        where T : class
    {
        var endpoint = await _provider.GetPublishEndpointAsync<T>(_consumeContext, default).ConfigureAwait(false);

        return new ScopedSendEndpoint(endpoint, _serviceProvider);
    }
}
