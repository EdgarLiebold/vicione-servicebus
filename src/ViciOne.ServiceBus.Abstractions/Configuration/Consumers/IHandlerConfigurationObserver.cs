
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for handler configuration observer.
/// </summary>
public interface IHandlerConfigurationObserver
{
    /// <summary>
    /// Called when a consumer/message combination is configured
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="configurator"></param>
    void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class;
}
