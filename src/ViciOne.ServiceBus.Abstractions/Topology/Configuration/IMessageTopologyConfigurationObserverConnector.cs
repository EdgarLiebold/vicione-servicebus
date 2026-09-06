namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by message topology configuration observer connector.</summary>
public interface IMessageTopologyConfigurationObserverConnector
{
    /// <summary>Connects message topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer);
}
