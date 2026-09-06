namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by bus observer connector.</summary>
public interface IBusObserverConnector
{
    /// <summary>Connects a bus observer to the bus to observe lifecycle events on the bus.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectBusObserver(IBusObserver observer);
}
