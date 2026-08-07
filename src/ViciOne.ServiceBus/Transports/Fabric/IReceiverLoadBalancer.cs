// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public interface IReceiverLoadBalancer<in T>
        where T : class
    {
        IMessageReceiver<T> SelectReceiver(T message);
    }
}
