
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for handler configuration observer connector.
/// </summary>
public interface IHandlerConfigurationObserverConnector
{
    /// <summary>
    /// Connects handler configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
}
