using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Defines the operations required by set property collection.</summary>
public interface ISetPropertyCollection :
    IPropertyCollection
{
    /// <summary>Sets a property.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The new value, or null to remove the property.</param>
    /// <returns>The set property collection produced by the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    ISetPropertyCollection Set(string key, string? value);

    /// <summary>Sets a property, overwriting an existing value if <paramref name="overwrite" /> is true.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The new value, or null to remove the property.</param>
    /// <param name="overwrite">The overwrite.</param>
    /// <returns>The set property collection produced by the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    ISetPropertyCollection Set(string key, object? value, bool overwrite = true);

    /// <summary>Set multiple properties from an existing collection, any null values a removed from the property collection.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="overwrite">The overwrite.</param>
    /// <returns>The set property collection produced by the operation.</returns>
    ISetPropertyCollection SetMany(IEnumerable<KeyValuePair<string, object?>>? properties, bool overwrite = true);
}
