namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configure a message handler, including specifying filters that are executed around
/// the handler itself.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IHandlerConfigurator<TMessage> :
    IConsumeConfigurator,
    IHandlerConfigurationObserverConnector,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}
