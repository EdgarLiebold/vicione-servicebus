namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send topology configuration observer.
/// </summary>
public interface ISendTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configuration">The configuration callback.</param>
    void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class;
}
