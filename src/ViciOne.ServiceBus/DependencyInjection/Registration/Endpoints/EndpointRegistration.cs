using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides an endpoint registration implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class EndpointRegistration<T> :
    IEndpointRegistration
    where T : class
{
    readonly IRegistration _registration;
    readonly IContainerSelector _selector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <param name="selector">The selector value.</param>
    public EndpointRegistration(IRegistration registration, IContainerSelector selector)
    {
        _registration = registration;
        _selector = selector;
    }

    /// <summary>
    /// Gets the type value.
    /// </summary>
    public Type Type => typeof(T);

    /// <summary>
    /// Gets or sets the include in configure endpoints value.
    /// </summary>
    public bool IncludeInConfigureEndpoints
    {
        get => _registration.IncludeInConfigureEndpoints;
        set { }
    }

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public IEndpointDefinition GetDefinition(IServiceProvider provider)
    {
        return _selector.GetEndpointDefinition<T>(provider)
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Endpoint Registration", "unknown", $"Endpoint definition not found: {TypeCache<T>.ShortName}", "Correct the named configuration before starting the host"));
    }
}
