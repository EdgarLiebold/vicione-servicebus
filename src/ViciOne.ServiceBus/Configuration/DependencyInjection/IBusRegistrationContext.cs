using System;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes state for bus registration operations.</summary>
public interface IBusRegistrationContext :
    IRegistrationContext
{
    /// <summary>Gets the bus contract represented by this registration context.</summary>
    Type BusType { get; }

    /// <summary>Gets the endpoint name formatter.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>
    /// Configure the endpoints for all defined consumer, saga, and activity types using an optional
    /// endpoint name formatter. If no endpoint name formatter is specified and an <see cref="IEndpointNameFormatter" />
    /// is registered in the container, it is resolved from the container. Otherwise, the <see cref="DefaultEndpointNameFormatter" />
    /// is used.
    /// </summary>
    /// <typeparam name="T">The bus factory type (depends upon the transport).</typeparam>
    /// <param name="configurator">The <see cref="IBusFactoryConfigurator" /> for the bus being configured.</param>
    /// <param name="endpointNameFormatter">Optional, the endpoint name formatter.</param>
    void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter)
        where T : IReceiveEndpointConfigurator;

    /// <summary>
    /// Configure the endpoints for all defined consumer, saga, and activity types using an optional
    /// endpoint name formatter. If no endpoint name formatter is specified and an <see cref="IEndpointNameFormatter" />
    /// is registered in the container, it is resolved from the container. Otherwise, the <see cref="DefaultEndpointNameFormatter" />
    /// is used.
    /// </summary>
    /// <typeparam name="T">The bus factory type (depends upon the transport).</typeparam>
    /// <param name="configurator">The <see cref="IBusFactoryConfigurator" /> for the bus being configured.</param>
    /// <param name="endpointNameFormatter">Optional, the endpoint name formatter.</param>
    /// <param name="configureFilter">A filter for the endpoints to be configured.</param>
    void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRegistrationFilterConfigurator>? configureFilter)
        where T : IReceiveEndpointConfigurator;

    /// <summary>
    /// Returns the registered <see cref="IConfigureReceiveEndpoint" /> instances from the container. Used internally
    /// to apply configuration to every receive endpoint. This method should normally not be called.
    /// </summary>
    /// <returns>The configure receive endpoints.</returns>
    IConfigureReceiveEndpoint GetConfigureReceiveEndpoints();
}
