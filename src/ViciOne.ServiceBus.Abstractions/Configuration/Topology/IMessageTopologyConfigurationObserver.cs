namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes the configuration of message-specific topology.</summary>
public interface IMessageTopologyConfigurationObserver
{
    /// <summary>Configures newly created entity-name topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configuration">The newly created message topology.</param>
    void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
        where T : class;
}
