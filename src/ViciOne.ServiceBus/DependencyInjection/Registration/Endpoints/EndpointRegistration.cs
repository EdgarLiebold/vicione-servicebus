using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registers endpoint services.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class EndpointRegistration<T> :
    IEndpointRegistration
    where T : class
{
    readonly IRegistration? _registration;
    readonly IContainerSelector _selector;
    bool _includeInConfigureEndpoints = true;

    internal EndpointRegistration(IContainerSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="registration">The registration.</param>
    /// <param name="selector">The selector.</param>
    public EndpointRegistration(IRegistration registration, IContainerSelector selector)
    {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(T);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints
    {
        get => _registration?.IncludeInConfigureEndpoints ?? _includeInConfigureEndpoints;
        set
        {
            if (_registration == null)
                _includeInConfigureEndpoints = value;
            else
                _registration.IncludeInConfigureEndpoints = value;
        }
    }

    /// <summary>Gets definition.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The definition.</returns>
    public IEndpointDefinition GetDefinition(IServiceProvider provider)
    {
        return _selector.GetEndpointDefinition<T>(provider)
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Endpoint Registration", "unknown", $"Endpoint definition not found: {TypeCache<T>.ShortName}", "Correct the named configuration before starting the host"));
    }
}
