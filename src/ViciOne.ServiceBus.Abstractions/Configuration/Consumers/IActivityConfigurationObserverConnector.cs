namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects observers that receive activity-configuration notifications.</summary>
public interface IActivityConfigurationObserverConnector
{
    /// <summary>Registers an activity-configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer);
}
