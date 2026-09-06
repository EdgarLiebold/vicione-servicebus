namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes the configuration of message-specific topology.</summary>
public interface IMessageTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The callback used to configure the component.</param>
    void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
        where T : class;
}
