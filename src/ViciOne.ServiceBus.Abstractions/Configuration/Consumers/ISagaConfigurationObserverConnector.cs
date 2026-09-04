
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga configuration observer connector.
/// </summary>
public interface ISagaConfigurationObserverConnector
{
    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer);
}
