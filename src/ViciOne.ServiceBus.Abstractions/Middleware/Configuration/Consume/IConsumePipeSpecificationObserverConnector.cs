namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume pipe specification observer connector.
/// </summary>
public interface IConsumePipeSpecificationObserverConnector
{
    /// <summary>
    /// Connects consume pipe specification observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer);
}
