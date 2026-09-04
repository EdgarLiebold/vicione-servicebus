
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer configuration observer connector.
/// </summary>
public interface IConsumerConfigurationObserverConnector
{
    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer);
}
