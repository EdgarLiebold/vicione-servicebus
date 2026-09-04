namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish topology configuration observer.
/// </summary>
public interface IPublishTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class;
}
