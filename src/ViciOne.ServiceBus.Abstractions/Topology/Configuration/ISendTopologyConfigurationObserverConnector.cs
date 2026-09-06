namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by send topology configuration observer connector.</summary>
public interface ISendTopologyConfigurationObserverConnector
{
    /// <summary>Connects send topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer);
}
