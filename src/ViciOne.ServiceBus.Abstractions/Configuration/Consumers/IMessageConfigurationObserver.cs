namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message configuration observer.
/// </summary>
public interface IMessageConfigurationObserver
{
    /// <summary>
    /// Called when a message pipeline is configured, for the very first time
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="configurator"></param>
    void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class;
}
