namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by endpoint configuration observer connector.</summary>
public interface IEndpointConfigurationObserverConnector
{
    /// <summary>Connect a configuration observer to the bus configurator, which is invoked as consumers are configured.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer);
}
