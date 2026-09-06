using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Describes one handler registration that contributes to a receive endpoint.</summary>
public interface IConsumerKindRegistration
{
    /// <summary>Gets the registered handler type.</summary>
    Type RegistrationType { get; }

    /// <summary>Gets the definition that owns the endpoint contribution.</summary>
    IDefinition Definition { get; }

    /// <summary>Gets the endpoint name selected for the registration.</summary>
    string EndpointName { get; }

    /// <summary>Gets the endpoint definition supplied by the registration, if any.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Gets whether the endpoint must be hosted by a service instance.</summary>
    bool RequiresServiceInstance { get; }

    /// <summary>Gets endpoint names materialized as companion endpoints.</summary>
    IReadOnlyCollection<string> CompanionEndpointNames { get; }

    /// <summary>Configures the registration on its materialized endpoint.</summary>
    void Configure(IConsumerKindEndpointContext context);
}
