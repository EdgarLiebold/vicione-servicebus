namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume topology configuration observer.
/// </summary>
public interface IConsumeTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configuration">The configuration callback.</param>
    void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
        where T : class;
}
