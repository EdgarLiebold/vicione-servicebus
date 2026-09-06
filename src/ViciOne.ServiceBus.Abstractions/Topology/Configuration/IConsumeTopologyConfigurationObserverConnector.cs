namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by consume topology configuration observer connector.</summary>
public interface IConsumeTopologyConfigurationObserverConnector
{
    /// <summary>Connects consume topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer);
}
