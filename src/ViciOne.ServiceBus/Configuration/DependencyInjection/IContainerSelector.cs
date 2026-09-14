using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves bus-owner-specific registrations and definitions from a built service provider.</summary>
public interface IContainerSelector
{
    /// <summary>Returns the registration from the service provider, if it exists.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <param name="provider">The service provider that contains the registrations.</param>
    /// <param name="type">The consumer, saga, activity, or other component type.</param>
    /// <param name="value">Receives the matching registration when found.</param>
    /// <returns><see langword="true" /> when a matching registration exists; otherwise, <see langword="false" />.</returns>
    bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration;

    /// <summary>Returns all registrations of a category for this bus owner.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="provider">The service provider that contains the registrations.</param>
    /// <returns>The registrations owned by the selected bus.</returns>
    IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        where T : class, IRegistration;

    /// <summary>Returns the definition from the service provider, if it exists.</summary>
    /// <typeparam name="T">The definition type.</typeparam>
    /// <param name="provider">The service provider that contains the definition.</param>
    /// <returns>The selected definition, or <see langword="null" /> when none is registered.</returns>
    T? GetDefinition<T>(IServiceProvider provider)
        where T : class, IDefinition;

    /// <summary>Returns the endpoint definition from the service provider, if it exists.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="provider">The service provider that contains the definition.</param>
    /// <returns>The selected endpoint definition, or <see langword="null" /> when none is registered.</returns>
    IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
        where T : class;

    /// <summary>Composes the global and bus-specific receive-endpoint callbacks.</summary>
    /// <param name="provider">The service provider that contains the callbacks.</param>
    /// <returns>A single callback that invokes every applicable endpoint callback.</returns>
    IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider);

    /// <summary>Returns the endpoint name formatter registered for the bus instance.</summary>
    /// <param name="provider">The service provider that contains the optional naming convention.</param>
    /// <returns>The bus-specific formatter or the default naming convention.</returns>
    IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider);
}
