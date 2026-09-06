namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive transport observer connector.</summary>
public interface IReceiveTransportObserverConnector
{
    /// <summary>Connects receive transport observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer);
}
