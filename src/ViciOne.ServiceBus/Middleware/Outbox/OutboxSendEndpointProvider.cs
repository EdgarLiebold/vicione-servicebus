using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public class OutboxSendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly OutboxSendContext _outboxContext;
    readonly ISendEndpointProvider _sendEndpointProvider;

    public OutboxSendEndpointProvider(OutboxSendContext outboxContext, ISendEndpointProvider sendEndpointProvider)
    {
        _outboxContext = outboxContext;
        _sendEndpointProvider = sendEndpointProvider;
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendEndpointProvider.ConnectSendObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_sendEndpointProvider);

    public async Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(address).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
