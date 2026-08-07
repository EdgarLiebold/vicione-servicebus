// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public interface IMessageFabricObserverConnector<out TContext>
        where TContext : class
    {
        ConnectHandle ConnectMessageFabricObserver(IMessageFabricObserver<TContext> observer);
    }
}
