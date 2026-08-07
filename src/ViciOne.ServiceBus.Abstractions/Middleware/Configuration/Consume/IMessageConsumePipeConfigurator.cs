// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// Configures the Consuming of a message type, allowing filters to be applied
    /// on Consume.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageConsumePipeConfigurator<TMessage> :
        IPipeConfigurator<ConsumeContext<TMessage>>
        where TMessage : class
    {
    }
}
