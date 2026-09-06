namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about message configuration events.</summary>
public interface IMessageConfigurationObserver
{
    /// <summary>Called when a message pipeline is configured, for the very first time.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class;
}
