namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about send topology configuration events.</summary>
public interface ISendTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The callback used to configure the component.</param>
    void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class;
}
