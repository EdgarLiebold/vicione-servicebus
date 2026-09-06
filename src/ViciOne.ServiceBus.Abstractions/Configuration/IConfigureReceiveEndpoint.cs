namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Implement this interface, and register the implementation in the container as the interface
/// type to apply configuration to all configured receive endpoints.
/// </summary>
public interface IConfigureReceiveEndpoint
{
    /// <summary>Configure the receive endpoint (called prior to any consumer, saga, or activity configuration).</summary>
    /// <param name="name">The name.</param>
    /// <param name="configurator">The configurator to update.</param>
    void Configure(string? name, IReceiveEndpointConfigurator configurator);
}
