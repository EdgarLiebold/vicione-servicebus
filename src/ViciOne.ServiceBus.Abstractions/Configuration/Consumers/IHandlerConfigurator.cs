// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Configure a message handler, including specifying filters that are executed around
    /// the handler itself
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IHandlerConfigurator<TMessage> :
        IConsumeConfigurator,
        IHandlerConfigurationObserverConnector,
        IPipeConfigurator<ConsumeContext<TMessage>>
        where TMessage : class
    {
    }
}
