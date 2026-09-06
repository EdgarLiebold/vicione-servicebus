using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Describes one handler registration that contributes to a receive endpoint.</summary>
public interface IConsumerKindRegistration
{
    /// <summary>Gets the registration type.</summary>
    Type RegistrationType { get; }

    /// <summary>Gets the definition.</summary>
    IDefinition Definition { get; }

    /// <summary>Gets the endpoint name.</summary>
    string EndpointName { get; }

    /// <summary>Gets the endpoint definition.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Gets whether the endpoint must be hosted by a service instance.</summary>
    bool RequiresServiceInstance { get; }

    /// <summary>Gets endpoint names materialized as companion endpoints.</summary>
    IReadOnlyCollection<string> CompanionEndpointNames { get; }

    /// <summary>Configures the registration on its materialized endpoint.</summary>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IConsumerKindEndpointContext context);
}
