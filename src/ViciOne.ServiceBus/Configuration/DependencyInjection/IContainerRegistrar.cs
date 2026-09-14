using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers bus components in a container and exposes their pre-build registration metadata.</summary>
public interface IContainerRegistrar :
    IContainerSelector
{
    /// <summary>Registers a request client that resolves destinations by message topology.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The effective default timeout for the client.</param>
    void RegisterRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>Registers a request client bound to an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">The effective default timeout for the client.</param>
    void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Registers the bus-owned scoped client factory.</summary>
    void RegisterScopedClientFactory();

    /// <summary>Registers the endpoint naming convention for this bus owner.</summary>
    /// <param name="endpointNameFormatter">The naming convention to register.</param>
    void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);

    /// <summary>Gets or adds a registration from the service collection.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <param name="type">The component type that identifies the registration.</param>
    /// <param name="missingRegistrationFactory">The factory used only when the registration does not exist.</param>
    /// <returns>The existing or newly created registration.</returns>
    T GetOrAddRegistration<T>(Type type, Func<Type, T>? missingRegistrationFactory = default)
        where T : class, IRegistration;

    /// <summary>Returns registrations from the service collection, prior to container construction.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <returns>The registrations.</returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;

    /// <summary>Registers a component definition and its service contract.</summary>
    /// <typeparam name="T">The definition service contract.</typeparam>
    /// <typeparam name="TDefinition">The concrete definition implementation.</typeparam>
    void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T;

    /// <summary>Registers an endpoint definition for a component.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <param name="settings">Optional settings passed explicitly to the definition constructor.</param>
    void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>;
}
