// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public interface IMessageQueue<in TContext, T> :
        IMessageSink<T>
        where TContext : class
        where T : class
    {
        string Name { get; }

        TopologyHandle ConnectMessageReceiver(TContext nodeContext, IMessageReceiver<T> receiver);
    }
}
