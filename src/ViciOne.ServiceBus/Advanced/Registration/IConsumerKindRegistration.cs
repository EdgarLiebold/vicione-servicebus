using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Describes one handler registration that contributes to a receive endpoint.</summary>
public interface IConsumerKindRegistration
{
    /// <summary>Gets the registered handler type.</summary>
    Type RegistrationType { get; }

    /// <summary>Gets the handler definition that contributes endpoint settings.</summary>
    IDefinition Definition { get; }

    /// <summary>Gets the name of the primary endpoint.</summary>
    string EndpointName { get; }

    /// <summary>Gets optional explicit settings for the primary endpoint.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Gets a value indicating whether the primary endpoint must be hosted by a service instance.</summary>
    bool RequiresServiceInstance { get; }

    /// <summary>Gets endpoint names materialized as companion endpoints.</summary>
    IReadOnlyCollection<string> CompanionEndpointNames { get; }

    /// <summary>Configures the registration on its materialized endpoint.</summary>
    /// <param name="context">The materialized endpoint context.</param>
    void Configure(IConsumerKindEndpointContext context);
}
