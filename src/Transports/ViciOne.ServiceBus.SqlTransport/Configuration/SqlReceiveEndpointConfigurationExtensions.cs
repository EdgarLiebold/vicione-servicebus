using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides extension methods for sql receive endpoint configuration.</summary>
public static class SqlReceiveEndpointConfigurationExtensions
{
    /// <summary>
    /// Declares a receive endpoint using a temporary definition with a generated name and an optional
    /// configuration callback.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ReceiveEndpoint(this ISqlBusFactoryConfigurator configurator, Action<ISqlReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Declare a receive endpoint using the endpoint <paramref name="definition"/>.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="definition">The definition.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ReceiveEndpoint(this ISqlBusFactoryConfigurator configurator, IEndpointDefinition definition,
        Action<ISqlReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
