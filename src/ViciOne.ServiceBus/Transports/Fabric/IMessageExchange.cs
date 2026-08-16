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
