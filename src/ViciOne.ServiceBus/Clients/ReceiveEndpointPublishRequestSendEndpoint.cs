namespace ViciOne.ServiceBus.Clients;

using System.Threading.Tasks;


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

        return await ready.ReceiveEndpoint.GetPublishSendEndpoint<TRequest>().ConfigureAwait(false);
    }
}
