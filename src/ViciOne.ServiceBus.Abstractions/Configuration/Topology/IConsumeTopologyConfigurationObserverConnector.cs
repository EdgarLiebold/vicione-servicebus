namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers observers for newly created consume topology.</summary>
public interface IConsumeTopologyConfigurationObserverConnector
{
    /// <summary>Registers a consume topology observer.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer);
}
