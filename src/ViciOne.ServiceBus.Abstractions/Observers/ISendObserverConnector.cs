namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Connect an observer that is notified when a message is sent to an endpoint.</summary>
public interface ISendObserverConnector
{
    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSendObserver(ISendObserver observer);
}
