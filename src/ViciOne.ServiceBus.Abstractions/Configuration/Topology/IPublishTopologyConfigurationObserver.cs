namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about publish topology configuration events.</summary>
public interface IPublishTopologyConfigurationObserver
{
    /// <summary>Configures newly created publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="configurator">The newly created message topology.</param>
    void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class;
}
