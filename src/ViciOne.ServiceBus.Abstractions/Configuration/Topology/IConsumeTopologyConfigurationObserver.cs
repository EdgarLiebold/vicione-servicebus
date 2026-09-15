namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about consume topology configuration events.</summary>
public interface IConsumeTopologyConfigurationObserver
{
    /// <summary>Configures newly created consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="configuration">The newly created message topology.</param>
    void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
        where T : class;
}
