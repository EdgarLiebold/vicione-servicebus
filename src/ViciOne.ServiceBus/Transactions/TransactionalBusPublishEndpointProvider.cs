// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transactions
{
    using System.Threading.Tasks;


    public class TransactionalBusPublishEndpointProvider :
        IPublishEndpointProvider
    {
        readonly BaseTransactionalBus _bus;
        readonly IPublishEndpointProvider _publishEndpointProvider;

        public TransactionalBusPublishEndpointProvider(BaseTransactionalBus bus, IPublishEndpointProvider publishEndpointProvider)
        {
            _bus = bus;
            _publishEndpointProvider = publishEndpointProvider;
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            return _publishEndpointProvider.ConnectPublishObserver(observer);
        }

        public async Task<ISendEndpoint> GetPublishSendEndpoint<T>()
            where T : class
        {
            var endpoint = await _publishEndpointProvider.GetPublishSendEndpoint<T>().ConfigureAwait(false);

            return new TransactionalBusSendEndpoint(_bus, endpoint);
        }
    }
}
