using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached access to read only property data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReadOnlyPropertyCache<T> : IEnumerable<ReadOnlyProperty<T>>
{
    /// <summary>Attempts to get value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value);
}
