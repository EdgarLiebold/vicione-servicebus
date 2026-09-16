using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped consume send endpoint services.</summary>
public class ScopedConsumeSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly ConsumeContext _consumeContext;
    readonly ISendEndpointProvider _provider;
    readonly IServiceProvider _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="scope">The scope.</param>
    public ScopedConsumeSendEndpointProvider(ISendEndpointProvider provider, ConsumeContext consumeContext, IServiceProvider scope)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(scope);

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
        var endpoint = await _provider.GetSendEndpointAsync(
            _consumeContext,
            address,
            requestId: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ScopedSendEndpoint(endpoint, _scope);
    }
}
