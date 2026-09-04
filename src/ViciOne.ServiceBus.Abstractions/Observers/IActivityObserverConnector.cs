namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for activity observer connector.
/// </summary>
public interface IActivityObserverConnector
{
    /// <summary>
    /// Connects activity observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectActivityObserver(IActivityObserver observer);
}
