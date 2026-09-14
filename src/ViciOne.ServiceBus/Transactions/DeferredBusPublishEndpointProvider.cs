using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transactions;

internal sealed class DeferredBusPublishEndpointProvider
{
    readonly DeferredBus _bus;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    public DeferredBusPublishEndpointProvider(DeferredBus bus, IPublishEndpointProvider publishEndpointProvider)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _publishEndpointProvider = publishEndpointProvider ?? throw new ArgumentNullException(nameof(publishEndpointProvider));
    }

    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ISendEndpoint endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<TMessage>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new DeferredBusSendEndpoint(_bus, endpoint);
    }
}
