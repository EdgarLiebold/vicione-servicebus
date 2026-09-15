namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers observers for newly created publish topology.</summary>
public interface IPublishTopologyConfigurationObserverConnector
{
    /// <summary>Registers a publish topology observer.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer);
}
