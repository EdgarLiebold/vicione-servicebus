namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about consume topology configuration events.</summary>
public interface IConsumeTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The callback used to configure the component.</param>
    void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
        where T : class;
}
