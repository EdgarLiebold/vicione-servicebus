using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by container registrar.</summary>
public interface IContainerRegistrar :
    IContainerSelector
{
    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    void RegisterRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Registers scoped client factory.</summary>
    void RegisterScopedClientFactory();

    /// <summary>Registers endpoint name formatter.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);

    /// <summary>Gets or adds a registration from the service collection.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="missingRegistrationFactory">The missing registration factory.</param>
    /// <returns>The or add registration.</returns>
    T GetOrAddRegistration<T>(Type type, Func<Type, T>? missingRegistrationFactory = default)
        where T : class, IRegistration;

    /// <summary>Returns registrations from the service collection, prior to container construction.</summary>
    /// <typeparam name="T">The registration type.</typeparam>
    /// <returns>The registrations.</returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;

    /// <summary>Gets or adds a definition from the service collection.</summary>
    /// <typeparam name="T">The definition type.</typeparam>
    /// <typeparam name="TDefinition">The definition implementation.</typeparam>
    void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T;

    /// <summary>Adds endpoint definition to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="settings">The settings that control the operation.</param>
    void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>;
}
