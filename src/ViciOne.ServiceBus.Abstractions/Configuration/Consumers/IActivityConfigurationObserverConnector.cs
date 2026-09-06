namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by activity configuration observer connector.</summary>
public interface IActivityConfigurationObserverConnector
{
    /// <summary>Connects activity configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer);
}
