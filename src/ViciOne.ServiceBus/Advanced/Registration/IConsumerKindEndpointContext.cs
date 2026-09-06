using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides a materialized endpoint and companion-endpoint factory to a consumer-kind registration.</summary>
public interface IConsumerKindEndpointContext
{
    /// <summary>Gets the services and registrations available to the active bus.</summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the materialized endpoint that receives this registration.</summary>
    IReceiveEndpointConfigurator EndpointConfigurator { get; }

    /// <summary>Materializes a companion endpoint required by the registration.</summary>
    /// <param name="endpointName">The unique companion endpoint name.</param>
    /// <param name="endpointDefinition">Optional settings for the companion endpoint.</param>
    /// <param name="configure">Configures the materialized companion endpoint.</param>
    void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
        Action<IReceiveEndpointConfigurator> configure);
}
