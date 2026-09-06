namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by retry observer connector.</summary>
public interface IRetryObserverConnector
{
    /// <summary>Connect an observer to the filter and/or pipe.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRetryObserver(IRetryObserver observer);
}
