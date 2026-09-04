using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

public class SendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly ISendEndpointProvider _provider;

    public SendRequestSendEndpoint(ISendEndpointProvider provider, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider;
        _destinationAddress = destinationAddress;
    }

    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetSendEndpointAsync(_destinationAddress).ConfigureAwait(false);
    }
}
