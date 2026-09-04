using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

public class PublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IPublishEndpointProvider _provider;

    public PublishRequestSendEndpoint(IPublishEndpointProvider provider, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _provider = provider;
    }

    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return await _provider.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
