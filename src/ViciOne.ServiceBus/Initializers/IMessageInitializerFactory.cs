// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers
{
    public interface IMessageInitializerFactory<TMessage>
        where TMessage : class
    {
        IMessageInitializer<TMessage> CreateMessageInitializer();
    }
}
