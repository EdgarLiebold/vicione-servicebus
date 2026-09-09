
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about handler configuration events.</summary>
public interface IHandlerConfigurationObserver
{
    /// <summary>Called when a message handler is configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The completed handler configuration.</param>
    void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class;
}
