namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for activity configuration observer connector.
/// </summary>
public interface IActivityConfigurationObserverConnector
{
    /// <summary>
    /// Connects activity configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer);
}
