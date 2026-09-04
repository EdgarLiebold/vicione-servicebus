namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint configuration observer connector.
/// </summary>
public interface IEndpointConfigurationObserverConnector
{
    /// <summary>
    /// Connect a configuration observer to the bus configurator, which is invoked as consumers are configured.
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer);
}
