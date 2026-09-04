namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint configuration observer.
/// </summary>
public interface IEndpointConfigurationObserver
{
    /// <summary>
    /// Called when an endpoint is configured
    /// </summary>
    /// <typeparam name="T">The receive endpoint configurator type</typeparam>
    /// <param name="configurator"></param>
    void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator;
}
