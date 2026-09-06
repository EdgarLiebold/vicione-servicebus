namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by publish topology configuration observer connector.</summary>
public interface IPublishTopologyConfigurationObserverConnector
{
    /// <summary>Connects publish topology configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer);
}
