namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about send topology configuration events.</summary>
public interface ISendTopologyConfigurationObserver
{
    /// <summary>Configures newly created send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="configuration">The newly created message topology.</param>
    void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class;
}
