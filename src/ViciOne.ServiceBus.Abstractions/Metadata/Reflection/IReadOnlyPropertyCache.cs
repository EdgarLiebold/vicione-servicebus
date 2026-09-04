using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for read only property cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IReadOnlyPropertyCache<T> : IEnumerable<ReadOnlyProperty<T>>
{
    /// <summary>
    /// Attempts to get value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value);
}
