using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides a materialized endpoint and companion-endpoint factory to a consumer-kind registration.</summary>
public interface IConsumerKindEndpointContext
{
    /// <summary>Gets the active bus registration context.</summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the primary receive endpoint configurator.</summary>
    IReceiveEndpointConfigurator EndpointConfigurator { get; }

    /// <summary>Materializes a companion endpoint required by the registration.</summary>
    void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
        Action<IReceiveEndpointConfigurator> configure);
}
