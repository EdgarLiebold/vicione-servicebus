using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for container registrar.
/// </summary>
public interface IContainerRegistrar :
    IContainerSelector
{
    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    void RegisterRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>
    /// Performs the register scoped client factory operation.
    /// </summary>
    void RegisterScopedClientFactory();

    /// <summary>
    /// Performs the register endpoint name formatter operation.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter);

    /// <summary>
    /// Gets or adds a registration from the service collection
    /// </summary>
    /// <param name="type"></param>
    /// <param name="missingRegistrationFactory"></param>
    /// <typeparam name="T">The registration type</typeparam>
    /// <returns></returns>
    T GetOrAddRegistration<T>(Type type, Func<Type, T>? missingRegistrationFactory = default)
        where T : class, IRegistration;

    /// <summary>
    /// Returns registrations from the service collection, prior to container construction
    /// </summary>
    /// <typeparam name="T">The registration type</typeparam>
    /// <returns></returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;

    /// <summary>
    /// Gets or adds a definition from the service collection
    /// </summary>
    /// <typeparam name="T">The definition type</typeparam>
    /// <typeparam name="TDefinition">The definition implementation</typeparam>
    /// <returns></returns>
    void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T;

    /// <summary>
    /// Adds endpoint definition to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <param name="settings">The settings value.</param>
    void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>;
}
