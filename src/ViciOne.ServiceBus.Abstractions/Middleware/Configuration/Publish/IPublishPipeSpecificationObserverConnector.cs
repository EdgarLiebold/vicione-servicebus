namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish pipe specification observer connector.
/// </summary>
public interface IPublishPipeSpecificationObserverConnector
{
    /// <summary>
    /// Connects publish pipe specification observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectPublishPipeSpecificationObserver(IPublishPipeSpecificationObserver observer);
}
