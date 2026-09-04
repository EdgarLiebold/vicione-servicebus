namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish topology configuration observer connector.
/// </summary>
public interface IPublishTopologyConfigurationObserverConnector
{
    /// <summary>
    /// Connects publish topology configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer);
}
