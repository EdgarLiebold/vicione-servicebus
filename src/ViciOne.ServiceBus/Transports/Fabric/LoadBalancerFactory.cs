// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public delegate IReceiverLoadBalancer<T> LoadBalancerFactory<T>(IMessageReceiver<T>[] consumers)
        where T : class;
}
