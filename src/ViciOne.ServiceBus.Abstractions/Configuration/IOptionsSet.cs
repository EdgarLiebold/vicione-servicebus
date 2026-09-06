using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by options set.</summary>
public interface IOptionsSet
{
    /// <summary>Configure the options, adding the option type if it is not present.</summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    T Options<T>(Action<T>? configure = null)
        where T : IOptions, new();

    /// <summary>Specify the options, will fault if it already exists.</summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions;

    /// <summary>Return the options, if present.</summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="options">Receives the options produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetOptions<T>(out T options)
        where T : IOptions;

    /// <summary>Enumerate the options which are assignable to the specified type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The selected options.</returns>
    IEnumerable<T> SelectOptions<T>()
        where T : class;
}
