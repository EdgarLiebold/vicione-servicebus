namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive transport observer connector.
/// </summary>
public interface IReceiveTransportObserverConnector
{
    /// <summary>
    /// Connects receive transport observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer);
}
