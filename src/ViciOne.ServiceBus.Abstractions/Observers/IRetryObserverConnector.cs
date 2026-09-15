namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Registers observers for retry lifecycle notifications.</summary>
public interface IRetryObserverConnector
{
    /// <summary>Connects an observer to the configured retry pipeline.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRetryObserver(IRetryObserver observer);
}
