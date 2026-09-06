using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides a materialized endpoint and companion-endpoint factory to a consumer-kind registration.</summary>
public interface IConsumerKindEndpointContext
{
    /// <summary>Gets the registration context.</summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the endpoint configurator.</summary>
    IReceiveEndpointConfigurator EndpointConfigurator { get; }

    /// <summary>Materializes a companion endpoint required by the registration.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
        Action<IReceiveEndpointConfigurator> configure);
}
