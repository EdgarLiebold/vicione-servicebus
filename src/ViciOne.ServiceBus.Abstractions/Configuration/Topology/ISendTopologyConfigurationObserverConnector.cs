namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers observers for newly created send topology.</summary>
public interface ISendTopologyConfigurationObserverConnector
{
    /// <summary>Registers a send topology observer.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer);
}
