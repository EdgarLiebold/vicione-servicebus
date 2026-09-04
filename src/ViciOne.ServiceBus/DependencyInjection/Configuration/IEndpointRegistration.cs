using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint registration.
/// </summary>
public interface IEndpointRegistration :
    IRegistration
{
    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    IEndpointDefinition GetDefinition(IServiceProvider provider);
}
