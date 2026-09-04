using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped consume send endpoint provider implementation.
/// </summary>
public class ScopedConsumeSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly ConsumeContext _consumeContext;
    readonly ISendEndpointProvider _provider;
    readonly IServiceProvider _scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="scope">The scope value.</param>
    public ScopedConsumeSendEndpointProvider(ISendEndpointProvider provider, ConsumeContext consumeContext, IServiceProvider scope)
    {
        _provider = provider;
        _consumeContext = consumeContext;
        _scope = scope;
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        return _provider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_provider);

    async Task<ISendEndpoint> ISendEndpointProvider.GetSendEndpointAsync(Uri address, CancellationToken cancellationToken)
    {
        var endpoint = await _provider.GetSendEndpointAsync(_consumeContext, address, default).ConfigureAwait(false);

        return new ScopedSendEndpoint(endpoint, _scope);
    }
}
