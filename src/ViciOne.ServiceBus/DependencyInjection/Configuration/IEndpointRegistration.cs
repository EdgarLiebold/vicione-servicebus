using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by endpoint registration.</summary>
public interface IEndpointRegistration :
    IRegistration
{
    /// <summary>Gets definition.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The definition.</returns>
    IEndpointDefinition GetDefinition(IServiceProvider provider);
}
