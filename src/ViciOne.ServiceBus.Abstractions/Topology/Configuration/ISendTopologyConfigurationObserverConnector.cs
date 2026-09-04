namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send topology configuration observer connector.
/// </summary>
public interface ISendTopologyConfigurationObserverConnector
{
    /// <summary>
    /// Connects send topology configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer);
}
