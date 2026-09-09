using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides case-insensitive lookup and enumeration of readable and writable instance properties.</summary>
/// <typeparam name="T">The declaring or derived target type.</typeparam>
public interface IReadWritePropertyCache<T> : IEnumerable<ReadWriteProperty<T>>
{
    /// <summary>Gets a cached property by name.</summary>
    /// <param name="name">The case-insensitive property name.</param>
    ReadWriteProperty<T> this[string name] { get; }

    /// <summary>Attempts to get a readable and writable property by name.</summary>
    /// <param name="key">The case-insensitive property name.</param>
    /// <param name="value">Receives the cached property when found.</param>
    /// <returns><see langword="true" /> when the property exists.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value);
}
