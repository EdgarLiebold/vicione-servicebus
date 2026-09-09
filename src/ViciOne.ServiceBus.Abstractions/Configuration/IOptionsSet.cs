using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and retrieves one configuration-options instance per exact option type.</summary>
public interface IOptionsSet
{
    /// <summary>Gets or creates the options of the requested type and applies optional configuration.</summary>
    /// <typeparam name="T">The configuration-options type.</typeparam>
    /// <param name="configure">An optional callback that configures the stored instance.</param>
    /// <returns>The unique options instance for <typeparamref name="T"/>.</returns>
    T Options<T>(Action<T>? configure = null)
        where T : IOptions, new();

    /// <summary>Adds a specific options instance and rejects a different instance for the same type.</summary>
    /// <typeparam name="T">The configuration-options type.</typeparam>
    /// <param name="options">The options instance to store.</param>
    /// <param name="configure">An optional callback that configures the stored instance.</param>
    /// <returns>The stored options instance.</returns>
    T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions;

    /// <summary>Attempts to get the options stored for the exact requested type.</summary>
    /// <typeparam name="T">The configuration-options type.</typeparam>
    /// <param name="options">Receives the stored options when found.</param>
    /// <returns><see langword="true" /> when options are present; otherwise, <see langword="false" />.</returns>
    bool TryGetOptions<T>(out T options)
        where T : IOptions;

    /// <summary>Enumerates stored options assignable to the requested type.</summary>
    /// <typeparam name="T">The base type or interface to select.</typeparam>
    /// <returns>The assignable stored options.</returns>
    IEnumerable<T> SelectOptions<T>()
        where T : class;
}
