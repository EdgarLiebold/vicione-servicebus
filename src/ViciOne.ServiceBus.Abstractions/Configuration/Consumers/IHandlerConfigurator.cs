namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the middleware pipeline surrounding a message handler.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IHandlerConfigurator<TMessage> :
    IConsumeConfigurator,
    IHandlerConfigurationObserverConnector,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}
