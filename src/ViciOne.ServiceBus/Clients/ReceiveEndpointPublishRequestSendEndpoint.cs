// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Clients;

using System.Threading.Tasks;
using Middleware;


public class ReceiveEndpointPublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly HostReceiveEndpointHandle _handle;

    public ReceiveEndpointPublishRequestSendEndpoint(HostReceiveEndpointHandle handle, ConsumeContext consumeContext)
        : base(consumeContext)
    {
        _handle = handle;
    }

    protected override async Task<ISendEndpoint> GetSendEndpoint()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        var endpoint = await ready.ReceiveEndpoint.GetPublishSendEndpoint<TRequest>().ConfigureAwait(false);

        return endpoint.SkipOutbox();
    }
}
