namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message topology configuration observer connector.
/// </summary>
public interface IMessageTopologyConfigurationObserverConnector
{
    /// <summary>
    /// Connects message topology configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer);
}
