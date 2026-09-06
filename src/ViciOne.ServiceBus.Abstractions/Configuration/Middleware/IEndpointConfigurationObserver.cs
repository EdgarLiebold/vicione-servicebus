namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about endpoint configuration events.</summary>
public interface IEndpointConfigurationObserver
{
    /// <summary>Called when an endpoint is configured.</summary>
    /// <typeparam name="T">The receive endpoint configurator type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator;
}
