// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Clients
{
    using System.Threading.Tasks;
    using Middleware;


    public class PublishRequestSendEndpoint<TRequest> :
        RequestSendEndpoint<TRequest>
        where TRequest : class
    {
        readonly IPublishEndpointProvider _provider;

        public PublishRequestSendEndpoint(IPublishEndpointProvider provider, ConsumeContext consumeContext)
            : base(consumeContext)
        {
            _provider = provider;
        }

        protected override async Task<ISendEndpoint> GetSendEndpoint()
        {
            var endpoint = await _provider.GetPublishSendEndpoint<TRequest>().ConfigureAwait(false);

            return endpoint.SkipOutbox();
        }
    }
}
