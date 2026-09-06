using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for receive endpoint configuration.</summary>
public static class ReceiveEndpointConfigurationExtensions
{
    /// <summary>Creates a temporary endpoint, with a dynamically generated name, that should be removed when the bus is stopped.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ReceiveEndpoint(this IBusFactoryConfigurator configurator, Action<IReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Creates a management endpoint which can be used by controllable filters on a bus instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="definition">The definition.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ReceiveEndpoint(this IBusFactoryConfigurator configurator, IEndpointDefinition definition, Action<IReceiveEndpointConfigurator>?
        configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
