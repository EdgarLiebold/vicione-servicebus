using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Used to pull configuration from the container, scoped to the bus, multi-bus, or mediator.</summary>
public interface IContainerSelector
{
    /// <summary>Returns the registration from the service provider, if it exists.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="type">The registration target type (Consumer, Saga, Activity, etc.).</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration;

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The registrations.</returns>
    IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        where T : class, IRegistration;

    /// <summary>Returns the definition from the service provider, if it exists.</summary>
    /// <typeparam name="T">The definition type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The definition, if found, otherwise null.</returns>
    T? GetDefinition<T>(IServiceProvider provider)
        where T : class, IDefinition;

    /// <summary>Returns the endpoint definition from the service provider, if it exists.</summary>
    /// <typeparam name="T">The definition target type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The endpoint definition.</returns>
    IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
        where T : class;

    /// <summary>Gets configure receive endpoints.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The configure receive endpoints.</returns>
    IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider);

    /// <summary>Returns the endpoint name formatter registered for the bus instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The endpoint name formatter.</returns>
    IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider);
}
