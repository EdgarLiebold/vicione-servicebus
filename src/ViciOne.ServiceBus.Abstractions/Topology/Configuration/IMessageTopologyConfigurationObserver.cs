namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Observes the configuration of message-specific topology
/// </summary>
public interface IMessageTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configuration">The configuration callback.</param>
    void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
        where T : class;
}
