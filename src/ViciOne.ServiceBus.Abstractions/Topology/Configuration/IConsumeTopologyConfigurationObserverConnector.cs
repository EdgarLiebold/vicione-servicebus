namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume topology configuration observer connector.
/// </summary>
public interface IConsumeTopologyConfigurationObserverConnector
{
    /// <summary>
    /// Connects consume topology configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer);
}
