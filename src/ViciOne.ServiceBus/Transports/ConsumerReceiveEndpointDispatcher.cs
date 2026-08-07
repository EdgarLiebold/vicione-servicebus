// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public class ConsumerReceiveEndpointDispatcher<T> :
        ITypeReceiveEndpointDispatcherFactory
        where T : class, IConsumer
    {
        public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
        {
            var queueName = formatter.Consumer<T>();

            return factory.CreateConsumerReceiver<T>(queueName);
        }
    }
}
