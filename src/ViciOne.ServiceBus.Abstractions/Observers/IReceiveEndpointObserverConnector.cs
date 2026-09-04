namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive endpoint observer connector.
/// </summary>
public interface IReceiveEndpointObserverConnector
{
    /// <summary>
    /// Connects receive endpoint observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer);
}
