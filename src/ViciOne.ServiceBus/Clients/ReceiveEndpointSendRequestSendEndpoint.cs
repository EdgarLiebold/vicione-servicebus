using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

public class ReceiveEndpointSendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly HostReceiveEndpointHandle _handle;

    public ReceiveEndpointSendRequestSendEndpoint(HostReceiveEndpointHandle handle, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle;
        _destinationAddress = destinationAddress;
    }

    protected override async Task<ISendEndpoint> GetSendEndpoint()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetSendEndpoint(_destinationAddress).ConfigureAwait(false);
    }
}
