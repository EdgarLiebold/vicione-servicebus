// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public class SingleReceiverLoadBalancer<T> :
        IReceiverLoadBalancer<T>
        where T : class
    {
        readonly IMessageReceiver<T> _receiver;

        public SingleReceiverLoadBalancer(IMessageReceiver<T> receiver)
        {
            _receiver = receiver;
        }

        public IMessageReceiver<T> SelectReceiver(T message)
        {
            return _receiver;
        }
    }
}
