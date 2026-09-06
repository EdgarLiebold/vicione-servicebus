namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive observer connector.</summary>
public interface IReceiveObserverConnector
{
    /// <summary>Connect an observer to the receiving endpoint.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectReceiveObserver(IReceiveObserver observer);
}
