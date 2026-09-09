namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the middleware pipeline that surrounds a consume observer.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IObserverConfigurator<TMessage> :
    IConsumeConfigurator,
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}
