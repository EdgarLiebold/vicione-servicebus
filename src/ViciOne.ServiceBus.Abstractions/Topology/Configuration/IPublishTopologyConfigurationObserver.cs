namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about publish topology configuration events.</summary>
public interface IPublishTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class;
}
