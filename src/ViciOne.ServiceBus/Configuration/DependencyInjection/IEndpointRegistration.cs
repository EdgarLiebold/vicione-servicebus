using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents an endpoint definition registered for a component type.</summary>
public interface IEndpointRegistration :
    IRegistration
{
    /// <summary>Resolves the endpoint definition for the selected bus owner.</summary>
    /// <param name="provider">The service provider that contains the definition.</param>
    /// <returns>The registered endpoint definition.</returns>
    IEndpointDefinition GetDefinition(IServiceProvider provider);
}
