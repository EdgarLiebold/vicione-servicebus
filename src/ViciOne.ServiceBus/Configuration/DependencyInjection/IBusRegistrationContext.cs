using System;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves bus-owned registrations and creates their convention-based receive endpoints.</summary>
public interface IBusRegistrationContext :
    IRegistrationContext
{
    /// <summary>Gets the bus contract represented by this registration context.</summary>
    Type BusType { get; }

    /// <summary>Gets the endpoint naming convention registered for this bus.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>
    /// Creates receive endpoints for every eligible consumer-kind and endpoint registration owned by this bus.
    /// </summary>
    /// <typeparam name="T">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The transport receive-endpoint factory.</param>
    /// <param name="endpointNameFormatter">An optional endpoint naming convention for this operation.</param>
    void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter = null)
        where T : IReceiveEndpointConfigurator;

    /// <summary>
    /// Creates receive endpoints for registrations selected by a caller-defined filter.
    /// </summary>
    /// <typeparam name="T">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The transport receive-endpoint factory.</param>
    /// <param name="endpointNameFormatter">An optional endpoint naming convention for this operation.</param>
    /// <param name="configureFilter">An optional callback that includes or excludes registrations.</param>
    void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRegistrationFilterConfigurator>? configureFilter)
        where T : IReceiveEndpointConfigurator;

    /// <summary>
    /// Returns the aggregate of global and bus-specific callbacks applied to each receive endpoint.
    /// </summary>
    /// <returns>The callback aggregate for this bus.</returns>
    IConfigureReceiveEndpoint GetConfigureReceiveEndpoints();
}
