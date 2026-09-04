namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Connect an observer that is notified when a message is sent to an endpoint
/// </summary>
public interface IPublishObserverConnector
{
    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectPublishObserver(IPublishObserver observer);
}
