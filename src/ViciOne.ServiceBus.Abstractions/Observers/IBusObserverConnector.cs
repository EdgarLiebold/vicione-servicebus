namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for bus observer connector.
/// </summary>
public interface IBusObserverConnector
{
    /// <summary>
    /// Connects a bus observer to the bus to observe lifecycle events on the bus
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectBusObserver(IBusObserver observer);
}
