using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides case-insensitive lookup and enumeration of readable instance properties.</summary>
/// <typeparam name="T">The declaring or derived target type.</typeparam>
public interface IReadOnlyPropertyCache<T> : IEnumerable<ReadOnlyProperty<T>>
{
    /// <summary>Attempts to get a readable property by name.</summary>
    /// <param name="key">The case-insensitive property name.</param>
    /// <param name="value">Receives the cached property when found.</param>
    /// <returns><see langword="true" /> when the property exists.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value);
}
