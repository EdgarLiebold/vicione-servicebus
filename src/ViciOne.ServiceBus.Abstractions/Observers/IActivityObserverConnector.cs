namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by activity observer connector.</summary>
public interface IActivityObserverConnector
{
    /// <summary>Connects activity observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectActivityObserver(IActivityObserver observer);
}
