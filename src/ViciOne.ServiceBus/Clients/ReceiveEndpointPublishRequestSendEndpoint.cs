using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

public class ReceiveEndpointPublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly HostReceiveEndpointHandle _handle;

    public ReceiveEndpointPublishRequestSendEndpoint(HostReceiveEndpointHandle handle, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle;
    }

    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
