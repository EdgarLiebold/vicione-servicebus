namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send pipe specification observer connector.
/// </summary>
public interface ISendPipeSpecificationObserverConnector
{
    /// <summary>
    /// Connects send pipe specification observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer);
}
