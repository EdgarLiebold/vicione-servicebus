namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers observers for newly created message entity-name topology.</summary>
public interface IMessageTopologyConfigurationObserverConnector
{
    /// <summary>Registers a message topology observer.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer);
}
