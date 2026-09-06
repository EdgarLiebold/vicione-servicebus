namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive endpoint observer connector.</summary>
public interface IReceiveEndpointObserverConnector
{
    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer);
}
