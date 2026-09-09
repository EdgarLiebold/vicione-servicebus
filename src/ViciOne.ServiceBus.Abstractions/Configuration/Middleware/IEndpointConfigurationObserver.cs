namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about endpoint configuration events.</summary>
public interface IEndpointConfigurationObserver
{
    /// <summary>Called after a receive endpoint's configuration is complete.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The completed endpoint configuration.</param>
    void EndpointConfigured<TEndpointConfigurator>(TEndpointConfigurator configurator)
        where TEndpointConfigurator : IReceiveEndpointConfigurator;
}
