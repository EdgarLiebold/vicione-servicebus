// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric
{
    public interface IMessageExchange<T> :
        IMessageSink<T>,
        IMessageSource<T>
        where T : class
    {
        string Name { get; }
    }
}
