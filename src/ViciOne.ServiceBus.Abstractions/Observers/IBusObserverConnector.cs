namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Connects observers that receive bus lifecycle notifications.</summary>
public interface IBusObserverConnector
{
    /// <summary>Registers an observer for construction, startup, and shutdown events.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>An idempotent handle that disconnects the registration.</returns>
    ConnectHandle ConnectBusObserver(IBusObserver observer);
}
