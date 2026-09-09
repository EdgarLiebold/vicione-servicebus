namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects observers that receive endpoint-configuration notifications.</summary>
public interface IEndpointConfigurationObserverConnector
{
    /// <summary>Registers an endpoint-configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer);
}
